# Security Policy

## Supported versions

Only the [latest release](https://github.com/cankhut/deadlock-server-picker/releases/latest)
gets fixes.

## Reporting a vulnerability

Please don't open a public issue for a security problem. Report it privately instead: open the
**Security** tab of this repository and click **Report a vulnerability**.

Include what you found, how to reproduce it, and your app version and OS. I'll reply as soon as
I can, and a confirmed problem gets fixed in a new release.

The app runs as administrator (or with sudo on Linux) and creates firewall rules, so these
count as security problems:

- the app changing or removing firewall rules it didn't create
- the app running commands or code from data it downloads, such as the server list
- anything that lets another program use the app's elevated rights
