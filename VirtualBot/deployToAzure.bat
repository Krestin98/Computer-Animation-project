@echo deleting zip
rem a way to zip deploy to azure "manually", change subscription, resource group, web app bot names respectively.
call del ./VirtualBot.zip
@echo Running deploy.cmd
call build.cmd
@echo Zipping all files...
7z a -r ./VirtualBot.zip ./*
@echo Login to Azure...
call az login
@echo setting subscription to ""
call az account set --subscription ""
@echo Deploying...
call az webapp deployment source config-zip --resource-group "USFCapStone" --name usfvatest3 --src ./VirtualBot.zip
@echo Done
@pause