﻿<Project Sdk = "Microsoft.NET.Sdk" >

  < PropertyGroup >
    < TargetFramework > netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include = "..\ArchBench.PlugIns\ArchBench.PlugIns.csproj" />
  </ ItemGroup >

  < ItemGroup >
    < Reference Include="HttpServer">
      <HintPath>..\HttpServer\HttpServer.dll</HintPath>
    </Reference>
  </ItemGroup>

</Project>
