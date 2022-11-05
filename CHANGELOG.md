# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2022-11-05
### Added
+ ABuildTool provides a template and useful methods for build tools
+ XcodeBuild lets us run xcodebuild tasks such as making an Archive of an iOS build
+ Versioning fills a VersionData asset based on tags and git log entries
+ VersioningSettings adds a Settings page to Unity to alter how VersionData is filled
+ VersionData assets contain enough data to locate which build they're included in, down to the commit