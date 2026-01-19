fastlane documentation
----

# Installation

Make sure you have the latest version of the Xcode command line tools installed:

```sh
xcode-select --install
```

For _fastlane_ installation instructions, see [Installing _fastlane_](https://docs.fastlane.tools/#installing-fastlane)

# Available Actions

## iOS

### ios upload_metadata

```sh
[bundle exec] fastlane ios upload_metadata
```

Upload all metadata to App Store Connect

### ios upload_screenshots

```sh
[bundle exec] fastlane ios upload_screenshots
```

Upload screenshots to App Store Connect

### ios upload_all

```sh
[bundle exec] fastlane ios upload_all
```

Upload both metadata and screenshots

### ios download_metadata

```sh
[bundle exec] fastlane ios download_metadata
```

Download current metadata from App Store Connect

### ios screenshots

```sh
[bundle exec] fastlane ios screenshots
```

Generate App Store screenshots using UI tests

### ios screenshots_upload

```sh
[bundle exec] fastlane ios screenshots_upload
```

Generate screenshots and upload to App Store Connect

### ios add_frames

```sh
[bundle exec] fastlane ios add_frames
```

Add device frames to screenshots

### ios create_app

```sh
[bundle exec] fastlane ios create_app
```

Create a new app on App Store Connect

### ios sync_app_info

```sh
[bundle exec] fastlane ios sync_app_info
```

Sync app information from App Store Connect

### ios test

```sh
[bundle exec] fastlane ios test
```

Run tests locally

### ios validate

```sh
[bundle exec] fastlane ios validate
```

Validate metadata before upload

### ios precheck_app

```sh
[bundle exec] fastlane ios precheck_app
```

Run precheck to validate App Store compliance

### ios version

```sh
[bundle exec] fastlane ios version
```

Print current app version from Xcode project

### ios bump_version

```sh
[bundle exec] fastlane ios bump_version
```

Increment version number

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
