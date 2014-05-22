For a full demonstration please view the screencast. http://screenr.com/VwuH

This package automatically uploads media file to Azure Blob Storage and can be easily switched between local and CDN.

The package will install 3 dlls and one example file under /Views/MacroPartials/ folder which shows how this package can be used.

Azure Blob Storage settings must be done on /config/FileSystemProviders.config file. here is the list of settings for Azure Blob Storage:

1. azureConnectionString : Azure Connection String

2. saveMediaToAzure : specify true if you wish to save media files to amazon. 

Make sure you create a container called "Media", the same name as "virtualRoot" in FileSystemProvider.Config file.

*** azure Connection string has to be in the following format:

DefaultEndpointsProtocol=https;AccountName=*****;AccountKey=****

It also adds two app config keys to web.config as follow:

1. "cdnDomain" this is the cdn domain of your website.

2. "useCDN" true/false. specify true in order to use cdnDomain before media url. (refer to the example file)

For a full demonstration please view the screencast. http://screenr.com/VwuHAST-Amazon-S3

