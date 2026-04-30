- cli dotnet tool that generates a typescript file exporting an interface and the associated injection token
- cli tool using System.CommandLine and Microsoft Extensions for DI, Logging, etc...
- accepts name of interface as an option in the command line
- can specify the output path optionally. Otherwise it is generated in the folder the tool was called.
- can be used on machine with .NET 8 installed and up
- can be published to Nuget




```example
import { InjectionToken } from '@angular/core';

export interface IFooService {

}

export const FOO_SERVICE =
  new InjectionToken<IFooService>('FOO_SERVICE');
```