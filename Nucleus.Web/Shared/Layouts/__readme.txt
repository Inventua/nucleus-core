The static resources in this directory are embedded in the Nucleus.Web.dll assembly.
The cshtml files are compiled and are included in the Nucleus.Web.dll assembly.

ALSO:
The cshtml files in this folder are copied to the published install set by the IncludeSharedLayoutAndContainerCshtmlFiles target in 
Properties/PublishProfiles/PublishInstall.pubxml and PublishUpgrade.pubxml.  This is required because the original source (cshtml) files 
for Layouts must be present so that Nucleus.Core.LayoutManager and Nucleus.Core.ContainerManager can parse them to identify layout 
Panes and container styles.