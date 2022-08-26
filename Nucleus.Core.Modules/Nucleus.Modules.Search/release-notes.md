## Release notes

### 1.0.0.1
- Implemented "basic page search" provider.  The basic search provider is very simple, and only searches pages by name, title, description and keywords.  It is suitable for testing, demo and very 
simple sites only.  Production environments should use the Elastic Search provider.
- Added support for multiple installed search providers and added a provider selection drop-down list to the settings page, along with selection logic in the controller search/suggest actions. 
- Improved behaviour when max suggestions is set to zero by preventing rendering of the suggestions `<div>` and javascript keypress event handling code.

### 1.0.0.0
26 August 2022:  Version 1.0.0.0 released.