NAnt -D:project.config=release -D:sign=true clean build-with-clover test >release-with-clover.log
NAnt -D:project.config=release -D:sign=true -D:vshik.installed=true -D:nunit2report.installed=false clean-bin package >release-package.log
