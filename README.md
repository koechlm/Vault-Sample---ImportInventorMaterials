# IMPORT MATERIAL JOB SAMPLE

This utility imports Inventor material definition names as list values for a user defined property in Vault.

INTRODUCTION:
---------------------------------
This utility imports Inventor material definition names as list values for a user defined property in Vault. 
The benefit of having all Inventor materials synchronized with Vault is to create/edit part file's or item's material property within Vault Client. 
All selected materials will comply with valid material definitions of Inventor. 

REQUIREMENTS:
---------------------------------
Vault Workgroup, Vault Professional 2019 or newer. This job leverages the Vault Inventor Server component and does not require Inventor installation or Inventor license.
The job is valid for any Vault configuration fulfilling these requirements:
- Enforce Workingfolder = Enabled
- Enforce Inventor Project File = Enabled
- Single project file in Vault.

TO CONFIGURE:
---------------------------------
1) Copy the folder Autodesk.ImportInvMaterialsSample to %ProgramData%\Autodesk\Vault 2016\Extensions\.
2) Check the Vault Behavior->Properties configuration for the UDP "Material": The utility is pre-configured to use the default property "Material". For localized Vault databases or different property used for material definition edit the Setting.xml.
Change the Value of attribute <mMatPropName> accordingly.
3) Add the job to the Job Queue either by executing the Powershell script (part of source code) or integrate into a custom lifecycle managing your *.adsklib files; add the Job-Type name
"Autodesk.ImportInvMaterialsSample" to the transition's 'Custom Job Types' tab.

DISCLAIMER:
---------------------------------
In any case all binaries, configuration code, templates and snippets of this solution is of "work in progress" character. This also applies to GitHub "Release" versions.
Neither Markus Koechl nor Autodesk represents that theses samples are reliable, accurate, complete, or otherwise valid. 
Accordingly, those configuration samples are provided “as is” with no warranty of any kind and you use the applications at your own risk.


NOTES/KNOWN ISSUES:
---------------------------------
The job expects that all library definition files configured in the Inventor project file are available in the file system. Otherwise, the job will fail and report "ob could not retrieve materials based on the ipj material library setting."

VERSION HISTORY / RELEASE NOTES:
---------------------------------
2019.24.1.0 - Initial Version
2019.24.2.0 - Added library file(s) download and validation against Inventor project file (ipj) settings.
2019.24.2.1 - Validation of library path and name against project file settings is now case insensitive

---------------------------------

Thank you for your interest in Autodesk Vault solutions and API samples.

Sincerely,

Markus Koechl, Autodesk
