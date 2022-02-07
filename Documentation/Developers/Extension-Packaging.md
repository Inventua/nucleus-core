A Nucleus extension is installed by installing a **package** using the `Extensions` page.

An extension package is a zip file which contains all of the files needed for your extension, along with a Extension Packaging (package.xml) file with instructions for Nucleus
on how to install your components. 

```
<?xml version="1.0" encoding="utf-8" ?>
<package id="{generate-guid}" xmlns="urn:nucleus/schemas/package/1.0">
  <name>Elastic Search</name>
  <version>1.0.0</version>
  <publisher name="Inventua" url="http://www.inventua.com" email="support@inventua.com" />
  <description>
    Sample Description.
  </description>
  <compatibility minVersion="1.0.0.0" maxVersion="1.99999.999999.99999" />
  
  <components>
    <component folderName="MyExtension" optional="false">
      <file name="readme.txt" />
      <file name="settings.css" />
      
      <folder name="Bin">
        <file name="Nucleus.Extensions.ElasticSearch.dll" />
        <file name="Nest.dll" />
        <file name="ElasticSearch.Net.dll" />
      </folder>
      
      <folder name="Views">
        <file name="Settings.cshtml" />
      </folder>
      
    </component>
  </components>
  
</package>
```