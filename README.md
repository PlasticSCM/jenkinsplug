# Jenkins plug

The Jenkins plug provides an interface to perform actions in a remote Jenkins
server for the Plastic SCM DevOps system.

This is the source code used by the actual built-in Jenkins plug. Use it as a reference
to build your own CI plug!

# Build
The executable is built from .NET Framework code using the provided `src/jenkinsplug.sln`
solution file. You can use Visual Studio or MSBuild to compile it.

**Note:** We'll use `${DEVOPS_DIR}` as alias for `%PROGRAMFILES%\PlasticSCM5\server\devops`
in *Windows* or `/var/lib/plasticscm/devops` in *macOS* or *Linux*.

# Setup
If you just want to use the built-in Jenkins plug you don't need to do any of this.
The Jenkins plug is available as a built-in plug in the DevOps section of the WebAdmin.
Open it up and configure your own!

## Configuration files
You'll notice some configuration files under `/src/configuration`. Here's what they do:
* `jenkinsplug.log.conf`: log4net configuration. The output log file is specified here. This file should be in the binaries output directory.
* `ci-jenkinsplug.definition.conf`: plug definition file. You'll need to place this file in the Plastic SCM DevOps directory to allow the system to discover your Jenkins plug.
* `jenkinsplug.config.template`: mergebot configuration template. It describes the expected format of the Jenkins plug configuration. We recommend to keep it in the binaries output directory
* `jenkinsplug.conf`: an example of a valid Jenkins plug configuration. It's built according to the `jenkinsplug.config.template` specification.

## Add to Plastic SCM Server DevOps
To allow Plastic SCM Server DevOps to discover your custom Jenkins plug, just drop 
the `ci-jenkinsplug.definition.conf` file in `${DEVOPS_DIR}/config/plugs/available`.
Make sure the `command` and `template` keys contain the appropriate values for
your deployment!

# Behavior
The **Jenkins plug** provides an API for **mergebots** to connect to Jenkins.
They use the plug to launch builds in a Jenkins server and retrieve the build status.

## What the configuration looks like
When a mergebot requires a CI plug to work, you can select a Jenkins Plug Configuration.

<p align="center">
  <img alt="CI plug select" src="https://raw.githubusercontent.com/mig42/jenkinsplug/master/doc/img/ci-plug-select.png" />
</p>

You can either select an existing configuration or create a new one.

When you create a new Jenkins Plug Configuration, you have to fill in the following values:

<p align="center">
  <img alt="Jenkinsplug configuration example"
       src="https://raw.githubusercontent.com/mig42/jenkinsplug/master/doc/img/configuration-example.png" />
</p>

## Installation requirements - The Jenkins Lightweight Plugin
**⚠️ Important! ⚠️**

Please make sure that you've installed our lightweight Jenkins plugin before you create
a new configuration for a server. You can find it in the Jenkins Plugin Manager
under the '*available*' tab (https://my.jenkins.server.net/pluginManager/available),
by the name 'Mergebot PlasticSCM Plugin'.

You'll also need to install a Plastic SCM CLI Client (version **7.0.16.2200** or higher)
in the Jenkins machine. It's required to perform all SCM operations against the server
(e.g. update the Jenkins Plastic SCM workspace). The user account running Jenkins will need
a valid Plastic SCM Client configuration to contact the target Plastic SCM Server.

## Jenkins Configuration
The lightweight Jenkins plugin makes it unnecessary to specify repositories in Jenkins.
Just select **Mergebot Plastic SCM** in the Source Code Management section. The
**mergebot** will take care of the rest!

<p align="center">
  <img alt="Plan repository"
       src="https://raw.githubusercontent.com/mig42/jenkinsplug/master/doc/img/project-configuration.png" />
</p>

When the **mergebot** requests a new build run or an existing build status
from the **Jenkins plug**, it calls the remote Jenkins API using the URL and
credentials in the plug configuration.

## How it works

When a user creates a new **Jenkins plug** configuration, by default it executes
the built-in plug binaries using the values of the configuration. Then, it automatically
connects to the Plastic SCM server through a *websocket* and stands by for requests.
You can also choose not to automatically run that particular configuration if you don't want to.

# Support
If you have any questions about this plug don't hesitate to contact us by
[email](support@codicesoftware.com) or in our [forum](http://www.plasticscm.net)!
