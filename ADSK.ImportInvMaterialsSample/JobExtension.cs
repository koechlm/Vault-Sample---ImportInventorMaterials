using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Connectivity.Extensibility.Framework;
using VDF = Autodesk.DataManagement.Client.Framework;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities;
using Autodesk.Connectivity.JobProcessor.Extensibility;
using Autodesk.Connectivity.WebServices;
using Inventor;

[assembly: ApiVersion("12.0")]
[assembly: ExtensionId("494c7aba-9cf2-4d8f-a3ca-75947a3a4cc4")]


namespace ADSK_ImportInvMaterialsSample
{
    public class JobExtension : IJobHandler
    {
        private static string JOB_TYPE = "Autodesk.ImportInvMaterialsSample";

        #region IJobHandler Implementation
        public bool CanProcess(string jobType)
        {
            return jobType == JOB_TYPE;
        }

        public JobOutcome Execute(IJobProcessorServices context, IJob job)
        {
            String mIpjPath = "";
            String mWfPath = "";
            String mIpjLocalPath = "";
            Autodesk.Connectivity.WebServices.File mFile;
            List<String> mMaterials = new List<String>();

            try
            {
                //read the settings to get individual names
                Settings settings = Settings.Load();
                if (settings.mMatPropName == null)
                {
                    context.Log("Material property name is not configured in Settings.xml", MessageType.eError);
                    return JobOutcome.Failure;
                }

                //Download enforced ipj file if not found
                Autodesk.Connectivity.WebServicesTools.WebServiceManager mWsMgr = context.Connection.WebServiceManager;
                if (mWsMgr.DocumentService.GetEnforceWorkingFolder() && mWsMgr.DocumentService.GetEnforceInventorProjectFile())
                {
                    mIpjPath = mWsMgr.DocumentService.GetInventorProjectFileLocation();
                    mWfPath = mWsMgr.DocumentService.GetRequiredWorkingFolderLocation();
                }
                else
                {
                    context.Log("Job requires both settings enabled: Enforce Workingfolder and Enforce Inventor Project", MessageType.eError);
                    return JobOutcome.Failure;
                }
                String[] mIpjFullFileName = mIpjPath.Split(new string[] { "/" }, StringSplitOptions.None);
                String mIpjFileName = mIpjFullFileName.LastOrDefault();

                //get the projects file object for download
                PropDef[] filePropDefs = mWsMgr.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                PropDef mNamePropDef = filePropDefs.Single(n => n.SysName == "ClientFileName");
                SrchCond mSrchCond = new SrchCond()
                {
                    PropDefId = mNamePropDef.Id,
                    PropTyp = PropertySearchType.SingleProperty,
                    SrchOper = 3, // is equal
                    SrchRule = SearchRuleType.Must,
                    SrchTxt = mIpjFileName
                };
                string bookmark = string.Empty;
                SrchStatus status = null;
                List<Autodesk.Connectivity.WebServices.File> totalResults = new List<Autodesk.Connectivity.WebServices.File>();
                while (status == null || totalResults.Count < status.TotalHits)
                {
                    Autodesk.Connectivity.WebServices.File[] results = mWsMgr.DocumentService.FindFilesBySearchConditions(new SrchCond[] { mSrchCond },
                        null, null, false, true, ref bookmark, out status);
                    if (results != null)
                        totalResults.AddRange(results);
                    else
                        break;
                }
                if (totalResults.Count == 1)
                {
                    mFile = totalResults[0];
                }
                else
                {
                    context.Log("Job execution stopped due to ambigous project file definitions; single project file per Vault expected", MessageType.eError);
                    return JobOutcome.Failure;
                }

                VDF.Vault.Settings.AcquireFilesSettings mDownloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(context.Connection);
                mDownloadSettings.LocalPath = new VDF.Currency.FolderPathAbsolute(mWfPath);
                VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(context.Connection, mFile);
                mDownloadSettings.AddFileToAcquire(fileIteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);

                VDF.Vault.Results.AcquireFilesResults mDownLoadResult = context.Connection.FileManager.AcquireFiles(mDownloadSettings);
                VDF.Vault.Results.FileAcquisitionResult fileAcquisitionResult = mDownLoadResult.FileResults.FirstOrDefault();
                mIpjLocalPath = fileAcquisitionResult.LocalPath.FullPath;

                //activate this Vault's ipj temporarily 
                try
                {
                    Inventor.InventorServer mInv = context.InventorObject as InventorServer;
                    Inventor.DesignProjectManager projectManager = mInv.DesignProjectManager;
                    Inventor.DesignProject mSaveProject = projectManager.ActiveDesignProject;
                    Inventor.DesignProject mProject = projectManager.DesignProjects.AddExisting(mIpjLocalPath);
                    mProject.Activate();
                    string mMatLibPath = mProject.ActiveMaterialLibrary.LibraryFilename;
                    Inventor.AssetLibraries mLibraries = mInv.AssetLibraries;
                    Inventor.AssetLibrary mLibrary = mInv.AssetLibraries.Open(mMatLibPath);
                    foreach (Inventor.MaterialAsset mMat in mLibrary.MaterialAssets)
                    {
                        mMaterials.Add(mMat.DisplayName);
                    }
                    mSaveProject.Activate();
                }
                catch (Exception)
                {
                    context.Log("Job could not retrieve materials based on the ipj material library setting.", MessageType.eError);
                    return JobOutcome.Failure;
                }

                //Get material UDP
                //Import list of materials to UDP
                //PropInstParam mPropInstParam = new PropInstParam();
                //PropInstParamArray mPropInstParamArray = new PropInstParamArray();
                //List<PropInstParam> mPropInstParamList = new List<PropInstParam>();
                //List<PropInstParamArray> mPropInstParamArrayArray = new List<PropInstParamArray>();

                //get the folder property definition Id using the displayname - toDo: create reusable method for that
                PropDef[] mPropDefs = context.Connection.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                PropDef mPropDef = mPropDefs.SingleOrDefault(n => n.DispName == settings.mMatPropName);
                PropDefInfo[] mPropDefInfo = mWsMgr.PropertyService.GetPropertyDefinitionInfosByEntityClassId("FILE", new long[] { mPropDef.Id });
                EntClassCtntSrcPropCfg[] mEntClassCtntSrcPropCfg = mPropDefInfo[0].EntClassCtntSrcPropCfgArray;
                PropConstr[] mPropConstrs = mPropDefInfo[0].PropConstrArray;
                //instead of reading existing values consume the new list of materials
                System.Object[] mListValues = mMaterials.ToArray(); //mPropDefInfo[0].ListValArray;
                PropDefInfo mUpdatedPropDefInfo = mWsMgr.PropertyService.UpdatePropertyDefinitionInfo(mPropDef, mEntClassCtntSrcPropCfg, mPropConstrs, mListValues);
                if (mUpdatedPropDefInfo != null)
                {
                    return JobOutcome.Success;
                }
                else
                {
                    context.Log("Job could not update the Material property definition in the final step.", MessageType.eError);
                    return JobOutcome.Failure;
                }
            }
            catch (Exception ex)
            {
                context.Log(ex, "Import Materials Job Sample failed: " + ex.ToString() + " ");
                return JobOutcome.Failure;
            }

        }

        public void OnJobProcessorShutdown(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }

        public void OnJobProcessorSleep(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }

        public void OnJobProcessorStartup(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }

        public void OnJobProcessorWake(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }
        #endregion IJobHandler Implementation
    }
}
