using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Connectivity.Extensibility.Framework;
using VDF = Autodesk.DataManagement.Client.Framework;
using Autodesk.Connectivity.JobProcessor.Extensibility;
using Autodesk.Connectivity.WebServices;
using Inventor;

[assembly: ApiVersion("16.0")]
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
            Autodesk.Connectivity.WebServices.File mProjFile;
            List<Autodesk.Connectivity.WebServices.File> mLibFiles;
            List<String> mMaterials = new List<String>();

            //------Start: Job Execution --------------
            try
            {
                Autodesk.Connectivity.WebServicesTools.WebServiceManager mWsMgr = context.Connection.WebServiceManager;

                //read the settings to get individual name for "Material" property
                Settings settings = Settings.Load();
                if (settings.mMatPropName == null)
                {
                    context.Log("Material property name is not configured in Settings.xml", MessageType.eError);
                    return JobOutcome.Failure;
                }

                //download and activate the Inventor Project file in VaultInventorServer, then download library files according ipj settings
                try
                {
                    //Download enforced ipj file if not found
                    
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
                        mProjFile = totalResults[0];
                    }
                    else
                    {
                        context.Log("Job execution stopped due to ambigous project file definitions; single project file per Vault expected", MessageType.eError);
                        return JobOutcome.Failure;
                    }

                    //define download settings for the project file
                    VDF.Vault.Settings.AcquireFilesSettings mDownloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(context.Connection);
                    mDownloadSettings.LocalPath = new VDF.Currency.FolderPathAbsolute(mWfPath);
                    VDF.Vault.Currency.Entities.FileIteration fileIteration = new VDF.Vault.Currency.Entities.FileIteration(context.Connection, mProjFile);
                    mDownloadSettings.AddFileToAcquire(fileIteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);

                    //download project file and get local path 
                    VDF.Vault.Results.AcquireFilesResults mDownLoadResult = context.Connection.FileManager.AcquireFiles(mDownloadSettings);
                    VDF.Vault.Results.FileAcquisitionResult fileAcquisitionResult = mDownLoadResult.FileResults.FirstOrDefault();
                    mIpjLocalPath = fileAcquisitionResult.LocalPath.FullPath;

                    //activate this Vault's ipj temporarily 
                    Inventor.InventorServer mInv = context.InventorObject as InventorServer;
                    Inventor.DesignProjectManager projectManager = mInv.DesignProjectManager;
                    Inventor.DesignProject mSaveProject = projectManager.ActiveDesignProject;
                    Inventor.DesignProject mProject = projectManager.DesignProjects.AddExisting(mIpjLocalPath);
                    mProject.Activate();

                    //------Start: read the active material library name and download all related files ---------
                    string mMatLibPath = mProject.ActiveMaterialLibrary.LibraryFilename;
                    string mMatLibName = mProject.ActiveMaterialLibrary.Name;

                    //search the library file(s) (*.adsklib + localization files *.xlf if used)
                    mSrchCond = new SrchCond()
                    {
                        PropDefId = mNamePropDef.Id,
                        PropTyp = PropertySearchType.SingleProperty,
                        SrchOper = 3, // is equal
                        SrchRule = SearchRuleType.Must,
                        SrchTxt = mMatLibName + "*",
                    };
                    bookmark = string.Empty;
                    status = null;
                    totalResults = new List<Autodesk.Connectivity.WebServices.File>();
                    while (status == null || totalResults.Count < status.TotalHits)
                    {
                        Autodesk.Connectivity.WebServices.File[] results = mWsMgr.DocumentService.FindFilesBySearchConditions(new SrchCond[] { mSrchCond },
                            null, null, false, true, ref bookmark, out status);
                        if (results != null)
                            totalResults.AddRange(results);
                        else
                            break;
                    }
                    if (totalResults.Count != 0)
                    {
                        mLibFiles = totalResults;
                    }
                    else
                    {
                        context.Log("Job execution stopped as no library file was found; as a minimum the library " + mMatLibName + ".adsklib is required according the project file setting.", MessageType.eError);
                        return JobOutcome.Failure;
                    }

                    //define download settings for library file
                    VDF.Vault.Settings.AcquireFilesSettings mDownloadSettings2 = new VDF.Vault.Settings.AcquireFilesSettings(context.Connection);
                    mDownloadSettings.LocalPath = new VDF.Currency.FolderPathAbsolute(mWfPath);
                    foreach (Autodesk.Connectivity.WebServices.File item in mLibFiles)
                    {
                        fileIteration = new VDF.Vault.Currency.Entities.FileIteration(context.Connection, item);
                        mDownloadSettings2.AddFileToAcquire(fileIteration, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
                    }

                    //download the library/libraries
                    mDownLoadResult = context.Connection.FileManager.AcquireFiles(mDownloadSettings2);

                    //validate mDownLoadResult comparing resulting path with given path in project file
                    bool mLibFound = false;
                    foreach (var item in mDownLoadResult.FileResults)
                    {
                        if (item.LocalPath.ToString().ToLower() == mMatLibPath.ToLower())
                        {
                            mLibFound = true;
                            break;
                        }
                    }
                    if (mLibFound != true)
                    {
                        context.Log("Job execution stopped as library "+ mMatLibName + ".adsklib downloaded to different location compared to given path in ipj setting.", MessageType.eError);
                        return JobOutcome.Failure;
                    }

                    //open the validated library and read material names
                    Inventor.AssetLibraries mLibraries = mInv.AssetLibraries;
                    Inventor.AssetLibrary mLibrary = mInv.AssetLibraries.Open(mMatLibPath);
                    foreach (Inventor.MaterialAsset mMat in mLibrary.MaterialAssets)
                    {
                        mMaterials.Add(mMat.DisplayName);
                    }

                    //switch temporarily used project file back to original one
                    mSaveProject.Activate();
                }
                catch (Exception)
                {
                    context.Log("Job could not retrieve materials based on the ipj material library setting. - Note: The ipj must not be checked out by another user.", MessageType.eError);
                    return JobOutcome.Failure;
                }
                //------End: read the active material library name and download all related files ---------

                //------Start: Fill UPD ----------------
                try
                {
                    //get the folder property definition Id using the displayname - toDo: create reusable method for that
                    PropDef[] mPropDefs = context.Connection.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                    PropDef mPropDef = mPropDefs.SingleOrDefault(n => n.DispName == settings.mMatPropName);
                    PropDefInfo[] mPropDefInfo = mWsMgr.PropertyService.GetPropertyDefinitionInfosByEntityClassId("FILE", new long[] { mPropDef.Id });
                    EntClassCtntSrcPropCfg[] mEntClassCtntSrcPropCfg = mPropDefInfo[0].EntClassCtntSrcPropCfgArray;
                    PropConstr[] mPropConstrs = mPropDefInfo[0].PropConstrArray;
                    
                    //sort the imported material listing and update UDP ListValues. 
                    mMaterials.Sort();
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
                    context.Log(ex, "Import Materials Job Sample failed during UDP update of ListValues: " + ex.ToString() + " ");
                    return JobOutcome.Failure;
                }
                //------End: Fill UPD ----------------
            }
            catch (Exception ex)
            {
                context.Log(ex, "Import Materials Job Sample failed for unhandled reason: " + ex.ToString() + " ");
                return JobOutcome.Failure;
            }
            //------End: Job Execution --------------
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
