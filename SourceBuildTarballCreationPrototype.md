# SourceBuild Tarball Creation with ArPow



## Source Organization:
- `src/SourceBuild/Arcade` - Contains source and tools for tarball build tasks & targets that will eventually move into Arcade but for expediency sake are built with the root repo.
- `src/tarball` - Contains the `BuildSourceBuildTarball.proj` which just triggers the tarball build target in Arcade.
- `src/tarball/content` - Contains all "legacy infra" files for building the tarball.  These get copied to the tarball verbatim.
- `eng/Build.props` - Updated to build source-build tasks and the BuildTarball project when `ArcadeBuildTarball` is true.

## Generating and Building a Tarball:
- To build the prototype:
    - Clone https://github.com/dseefeld/linker/tree/tarballCreation
    - Call `build.sh` with the following parameters
        - `/p:ArcadeBuildTarball=true` - Signals to Arcade that tarball is being built
        - `/p:TarballDir=<directory>` - (optional) Specified where the tarball is being created.  Currently requires an absolute path in prototype.  If not specified, tarball is generated in `artifacts\tarball`.  Note: Tarball shouldn't be built from `artifacts\tarball` dir since NuGet.Config above it may conflict?
    - Navigate to the tarball directory.
    - Call `prep.sh` to prep the tarball.
    - Call `build.sh` to build. 

## Build Steps:
- CreateSourceTarball target (this only clones source)
    - Create directory
    - Setup shared environment variables
    - Clone self into tarball directory
    - Get Dependent repos via Version.Details.xml
    - Clone Dependent repo into tarball directory
        - Modify `NuGet.Config` to include local packages source
        - Write `git-info` for repo
        - Recursively call clone target for dependent repo's Version.Details.xml

- Copy source from `source-build` repo for legacy build infra
- Copy `build.sh` and `prep.sh` from `support/tarball`
- Additional changes:
    - Copy `DarcVersion.txt` to tarball.  This shouldn't be needed because Darc is not being used in new tarball.
    - Copy `archiveArtifacts.txt` from temporary location to new tarball. This needs to be generated from props file in future tarballs.

## Assumptions

- The build is a tarball build only, but trying to keep online sources (for prebuilt detection)
- Arcade comes from previously source-built packages
- Each repo is building with ArPow build flags - i.e. is cloning to `self` and performing inner build
- Generated intermediate files are unpacked and provided in the global package cache (`PackageVersions.props` will be updated)


## Open Issues / Changes Needed

### Functional:
- **[Root Repo git-info](https://github.com/dotnet/source-build/issues/2250)** - The root repo doesn't have git-info because generation for that is in "clone dependent repo" and the info is generated from `Version.Details.xml`.  There isn't sha and version information available for the the root repo.  Where should this information exist?  In current source-build, this comes from the version of `Version.Details.xml` in source-build since it is an overarching repo.  In the current prototype, this information is hardcoded in `src/SourceBuild/Arcade/tools/SourceBuildArcadeTarball.targets`.
- **[Text-only Packages](https://github.com/dotnet/source-build/issues/2251)** - The prototype is currently not including text-only packages.  These need to be identified and included in the tarball without building first or a production build step is needed.
- **[Legacy source-build infra](https://github.com/dotnet/source-build/issues/2252)** - Where should the legacy source-build infra go?  In the prototype, it is under `src/SourceBuild/tarball/content` in the root repo.  This also needs cleanup, since it was all copied from the source-build repo verbatim and then modified to build the prototype, but there are targets and props that will no longer be required.
- **[SourceBuildArcadeTarball.targets](https://github.com/dotnet/source-build/issues/2253)** - The prototype uses targets from this file to generate the tarball.  It does not use `eng/SourceBuild.Tarball.targets` in the source-build repo.  The source-build method should be compared against the prototype process to ensure that all pieces are included or no longer needed.
- **[Inline code changes](https://github.com/dotnet/source-build/issues/2254)** - For some repos, there are code changes being made via sed, awk or a msbuild task with existing source-build.  Example: Finding and setting up [the arcade logger](https://github.com/dotnet/source-build/blob/cb88284e9266f33b86daff4b078678b6c316a9a9/repos/Directory.Build.targets#L92-L104).  These existing code changes should be reviewed and included in the new tarball if necessary.
- **[Production vs. Offline build](https://github.com/dotnet/source-build/issues/2255)** - Source-build will need the ability to build the tarball in an "online mode" to allow detection of prebuilts.  The current prototype always builds in this mode, but it needs to be selectable with a flag.
- **[Prebuilt checks](https://github.com/dotnet/source-build/issues/2256)** - In the prototype, prebuilt checks are being made in the inner build on a per-repo basis as well as an overall prebuilt check in the main tarball build.  These should be reconciled.  Could additional sources/caches be passed to the inner build to allow their prebuilt checks to be more accurate in a tarball build context?  This may be good for identifying per-repo prebuilts.  Also, is the main build prebuilt check accurate, or does it need to check inner build package caches as well?
- **[Source-build Tasks Build](https://github.com/dotnet/source-build/issues/2257)** - Tasks in `tools-local` require some reference packages from SBRP.  These are no longer available because SBRP is not included as a repo rather than as a separate archive that gets restored prior to anything building.  Perhaps a subset of the reference packages from SBRP need to be included in the previously source-built archive.
- **[DotNet Host Version](https://github.com/dotnet/source-build/issues/2258)** - The existing tarball build script has code to determine the DotNet Host version from PVP.  This sets DOTNET_HOST_BOOTSTRAP_VERSION.  It doesn't appear that this exists anymore.  It is removed in the prototype.
- **[Arcade Patches](https://github.com/dotnet/source-build/issues/2263)** - Arcade requires 2 additional patches to build successfully in the tarball prototype.  These patches need to be reviewed and if valid, should be integrated into Arcade.
- **[git-clone-to-dir.sh](https://github.com/dotnet/source-build/issues/2259)** - Arcade contains this script in the Arcade SDK under `tools/SourceBuild`.  When restoring arcade using the `ExtractToolPackage` target, it is not executable, which causes an issue with the inner build clone.  How is unzipping the package different than installing it with NuGet?

### Efficiency:
- **[Including .git dir](https://github.com/dotnet/source-build/issues/2260)** - The clone step clones all source into the tarball directory including their .git directory.  This is included to allow the inner build to clone and build.  This will result in larger tarballs because it's not just including source.  This needs to be investigated to determine if less can be included or if there is an alternative to cloning to do the inner build.
- **[Unpacking Intermediate Packages](https://github.com/dotnet/source-build/issues/2261)** - Intermediate packages are created from the inner build and then copied and unzipped into the blob-feed.  This is good separation between repo builds, but takes up more space.  If space becomes an issue, alternatives should be considered.
- **[Previously source-built packages](https://github.com/dotnet/source-build/issues/2262)** - Should previously source built be made up of only intermediate packages instead of all nupkgs to allow better compartmentalization?  A zip file of nupkgs of nupkgs! :)