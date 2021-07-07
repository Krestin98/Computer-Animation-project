@echo Running LUISGen
call luis export version --appId "6375e863-b12b-4669-9472-7f42e073222f" --versionId "0.1" --authoringKey "1396d1b50e7c4387a4eb14ec27d8170b" | luisgen --stdin -cs "USFVirtualAssistantLUIS"
@echo Done
@pause