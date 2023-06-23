Since we don't provide any guarantees about private APIs being stable whatsoever, you need to pin the exact version of Avalonia in your package dependencies, like this:
`<PackageReference Include="Avalonia" Version="[11.0.0-preview8]" />`

To proceed with private API usage, add 
`<Avalonia_I_Want_To_Use_Private_Apis_In_Nuget_Package_And_Promise_To_Pin_The_Exact_Avalonia_Version_In_Package_Dependency>true</Avalonia_I_Want_To_Use_Private_Apis_In_Nuget_Package_And_Promise_To_Pin_The_Exact_Avalonia_Version_In_Package_Dependency>` to your project file