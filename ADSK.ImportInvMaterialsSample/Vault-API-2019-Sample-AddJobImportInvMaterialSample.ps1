#region disclaimer
	#===============================================================================#
	# PowerShell script sample														#
	# Author: Markus Koechl															#
	# Copyright (c) Autodesk 2017													#
	#																				#
	# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER     #
	# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES   #
	# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.    #
	#===============================================================================#
#endregion

#region project settings
	# -------------- set the location of your powershell project accordingly -------------
	$MySolutionPath = 'C:\Users\koechlm\source\repos\VDS-Configuration-Editor' 
	# -------------- set the Vault Edition/Version Name used -------------
	$VaultVersion = 'Vault Professional 2019'
#endregion project settings

#region ConnectToVault
	Try
	{
		[System.Reflection.Assembly]::LoadFrom('C:\Program Files\Autodesk\' + $VaultVersion +'\Explorer\Autodesk.Connectivity.WebServices.dll')
		$serverID = New-Object Autodesk.Connectivity.WebServices.ServerIdentities
			$serverID.DataServer = "192.168.85.128"
			$serverID.FileServer = "192.168.85.128"
		$VaultName = "VLT-MKDE"
		$UserName = "Administrator"
		$password = ""
		#new in 2019 API: licensing agent enum "Client" "Server" or "None" (=readonly access). 2017 and 2018 required local client installed and licensed
		$licenseAgent = [Autodesk.Connectivity.WebServices.LicensingAgent]::Client
		
		$cred = New-Object Autodesk.Connectivity.WebServicesTools.UserPasswordCredentials($serverID, $VaultName, $UserName, $password, $licenseAgent)
		$vault = New-Object Autodesk.Connectivity.WebServicesTools.WebServiceManager($cred)

		#region ExecuteInVault
			Try
			{
				[Autodesk.Connectivity.WebServices.JobParam[]] $params = @()
				[System.Reflection.Assembly]::LoadFrom('C:\Program Files\Autodesk\' + $VaultVersion + '\Explorer\Autodesk.DataManagement.Client.Framework.Vault.dll')
				[System.Enum]$AuthFlag = [Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections.AuthenticationFlags]::Standard
				$UsrID = $vault.AuthService.CurrentlySetSecurityHeader.UserId				
				$vaultConnection = New-Object Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections.Connection($vault, $VaultName, $UsrID, $serverID, $AuthFlag )
				$vaultConnection.WebServiceManager.JobService.AddJob("Autodesk.ImportInvMaterialsSample", "Sample to Import Inventor Active Materials to Vault UDP", $params, 1)
				echo "Job successfully added to Queue"
			}
			Catch
			{
				$ErrorMessage = $_.Exception.Message
				echo $ErrorMessage
			}
		#endregion ExecuteInVault
	}
	Catch
	{
		$ErrorMessage = $_.Exception.Message
		echo $ErrorMessage
	}
#endregion ConnectToVault

#region DisconnectVault - don't forget ;)
	Finally
	{
		$vaultConnection = $null
		$vault.Dispose()
	}
#endregion DisconnectVault