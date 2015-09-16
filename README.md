This package automatically uploads media file to amazon S3. It also allows media files tobe loaded easily from S3 or CDN.

The package will install 3 dlls and one example file under /Views/MacroPartials/ folder which shows how this package can be used.

Amazon S3 settings must be done on /config/FileSystemProviders.config file. here is the list of settings for amazon:

1. awsAccessKey : Amazon Access Key

2. awsSecretKey : Amazon Secret Key

3. awsBucketName : S3 Bucket Name

4. awsSaveMediaToS3 : specify true if you wish to save media files to amazon, the default value is false. 

5. awsRegion : Match the region name of a Region Endpoint in the AWSSDK.

e.g.

```xml
<?xml version="1.0"?>
<FileSystemProviders>
  <Provider alias="media" type="AST.S3.FileSystem.S3FileSystem,AST.S3">
    <Parameters>
      <add key="virtualRoot" value="~/media/debug/" />
      <add key="awsAccessKey" value="[key]" />
      <add key="awsSecretKey" value="[secret]" />
      <add key="awsBucketName" value="[bucket]" />
      <add key="awsSaveMediaToS3" value="true" />
      <add key="awsRegion" value="EUWest1" />
    </Parameters>
  </Provider>
</FileSystemProviders>
```

The package also adds two app config keys to web.config as follow:

1. "cdnDomain" this is the cdn domain of your website. (must start with http ,  https or // and end with no forward slash)

2. "useCDN" true/false. specify true in order to use cdnDomain before media url. (refer to the example file). The default value is false.


* For existing project, just copy your media folder to Amazon S3 and install and use this package, as easy as that! 

For a full demonstration please view the screencast. http://screenr.com/P8NH

Your feedback is much appreciated. 