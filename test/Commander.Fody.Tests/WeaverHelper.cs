﻿using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;
using Commander.Fody;

public class WeaverHelper
{
    private readonly IAssemblyResolver _assemblyResolver;
    public string BeforeAssemblyPath;
    public string AfterAssemblyPath;
    public Assembly Assembly;


    public WeaverHelper(string assemblyName, string targetFramework = null, string extension = null)
    {
        targetFramework = targetFramework ?? "net462";
        extension = extension ?? ".dll";
        BeforeAssemblyPath = Path.GetFullPath(
            Path.Combine(
                TestContext.CurrentContext.TestDirectory, 
                $@"..\..\..\..\..\TestAssemblyBin\{targetFramework}", 
                assemblyName + extension)
            );

#if (RELEASE)
        BeforeAssemblyPath = BeforeAssemblyPath.Replace("Debug", "Release");
#endif        
        AfterAssemblyPath = BeforeAssemblyPath.Replace(extension, "2"+extension);
        
        File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);

        var assemblyResolver = new TestAssemblyResolver(targetFramework);
        _assemblyResolver = assemblyResolver;
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = assemblyResolver
        };
        using (var moduleDefinition = ModuleDefinition.ReadModule(BeforeAssemblyPath, readerParameters))
        {
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition,
                AssemblyResolver = assemblyResolver
            };

            weavingTask.Execute();

            moduleDefinition.Write(AfterAssemblyPath);
        }

        Assembly = Assembly.LoadFrom(AfterAssemblyPath);
    }

    public ModuleDefinition GetModuleDefinitionAfterWeave()
    {
        var moduleDefinition = ModuleDefinition.ReadModule(AfterAssemblyPath, new ReaderParameters {
            AssemblyResolver = _assemblyResolver
        });
        return moduleDefinition;
    }

    public ModuleDefinition GetModuleDefinitionBeforeWeave()
    {
        var moduleDefinition = ModuleDefinition.ReadModule(BeforeAssemblyPath, new ReaderParameters
        {
            AssemblyResolver = _assemblyResolver
        });
        return moduleDefinition;
    }
}