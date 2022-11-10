# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
+ VersionDisplay is a component to display VersionData to a TextMeshPro Text
+ PrebuildVersionUpdate updates the VersionData before each build

### Changed
+ Move Update Version Data menu item to top of Version Data submenu
+ Gather optional counts like Branch Count and number of uncommitted Changes into Bonus field  

### Fixed
+ error from asynchronous writing to linear stream in AddAppUsesExemptEncryption method of ABuildTool  

## [0.2.0] - 2022-11-05
### Added
+ ABuildTool provides a template and useful methods for build tools
+ XcodeBuild lets us run xcodebuild tasks such as making an Archive of an iOS build
+ Versioning fills a VersionData asset based on tags and git log entries
+ VersioningSettings adds a Settings page to Unity to alter how VersionData is filled
+ VersionData assets contain enough data to locate which build they're included in, down to the commit