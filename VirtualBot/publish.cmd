nuget restore
msbuild CoreBot.sln -p:DeployOnBuild=true -p:PublishProfile=usfvirtualassistant-Web-Deploy.pubxml -p:Password=npXG7Sxfo66CtZlkhtHPydWbpu9ysugvfqMbRym4qlZtB49kACpjWQf1NSRh
@pause
