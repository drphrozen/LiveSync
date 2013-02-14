LiveSync
========

Live synchronization of one directory to another.

This tool was created to sync code changes directly to my IIS server. It watches a directory for changes (recursively) and performs the same changes to a target directory.
The tool was made to only sync changes while it is running, this means that an initial sync must be performed to ensure that the structure is the same for the watch directory and the target directory.

Here's an example of the "LiveSync.xml":
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Settings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <WatchPath>c:\inetpub\wwwroot\DefaultWebSite\</WatchPath>
  <TargetPath>\\server\DefaultWebSite\</TargetPath>
  <Patterns>
    <Pattern>\.(js|aspx|less)$</Pattern>
    <Pattern>^Content\\</Pattern>
  </Patterns>
  <Overwrite>false</Overwrite>
  <ActivityDelay>1000</ActivityDelay>
</Settings>
```

Download
========
Download the latest build [here](https://docs.google.com/uc?export=download&id=0B3Yn35IwOdO5Mi1FM0dCMDhOZWM)
