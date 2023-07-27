# CommandAsSql 
[![Build status](https://ci.appveyor.com/api/projects/status/box9pmdwhj2srcoa?svg=true)](https://ci.appveyor.com/project/jphellemons/commandassql)

[![.NET Core Desktop](https://github.com/jphellemons/CommandAsSql/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/jphellemons/CommandAsSql/actions/workflows/dotnet-desktop.yml)

.Net standard 2.0 Nuget package. Extension method for SqlCommand to display all parameters as inline SQL. Output is a string which can be copy pasted in a DB management tool.
It's based on some code from StackOverflow and enhanced for table value parameters.
Please send pull-requests.

## Install in packagemanager:

> Install-Package CommandAsSql

## Install from CLI:

> dotnet add package CommandAsSql

## License

See License.md for the MIT License

## Other

Can be found on Nuget: https://www.nuget.org/packages/CommandAsSql/

Most code is from Flapper https://stackoverflow.com/users/391383/flapper
and his StackOverflow answer: https://stackoverflow.com/a/4146573/169714

![database icon](https://png.icons8.com/win8/1600/107C10/database)
