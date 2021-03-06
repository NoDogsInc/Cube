﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

class MethodDefinitionNotFound : Exception {
    public MethodDefinitionNotFound(string methodName)
        : base(methodName) { }
}

/// <summary>
/// 
/// </summary>
/// <remarks>Available in: Editor</remarks>
abstract class PostProcessor {
    protected ModuleDefinition MainModule {
        get;
        private set;
    }

    public PostProcessor(ModuleDefinition mainModule) {
        MainModule = mainModule;
    }

    public abstract bool Process(ModuleDefinition module);

    protected TypeReference Import(TypeDefinition type) {
        return MainModule.ImportReference(type);
    }

    protected MethodReference Import(MethodDefinition method) {
        return MainModule.ImportReference(method);
    }

    protected List<MethodReference> Import(IEnumerable<MethodDefinition> methods) {
        var results = new List<MethodReference>();
        foreach (var method in methods) {
            results.Add(Import(method));
        }
        return results;
    }

    protected FieldReference Import(FieldDefinition field) {
        return MainModule.ImportReference(field);
    }

    protected TypeDefinition GetTypeDefinitionByName(AssemblyDefinition assembly, string typeName) {
        return GetTypeDefinitionByName(assembly.MainModule, typeName);
    }

    protected TypeDefinition GetTypeDefinitionByName(ModuleDefinition module, string typeName) {
        return module.GetType(typeName);
    }

    protected MethodDefinition GetMethodDefinitionByName(TypeDefinition type, string methodName) {
        if (type == null)
            throw new ArgumentNullException("type");

        return type.Methods.FirstOrDefault(x => x.Name == methodName);
    }

    protected IEnumerable<MethodDefinition> GetMethodDefinitionsByName(TypeDefinition type, string methodName) {
        if (type == null)
            throw new ArgumentNullException("type");

        return type.Methods.Where(x => x.Name == methodName);
    }

    protected PropertyDefinition GetPropertyDefinitionByName(TypeDefinition type, string propertyName) {
        foreach (var property in type.Properties) {
            if (property.Name == propertyName)
                return property;
        }
        return null;
    }

    protected FieldDefinition GetFieldDefinitionByName(TypeDefinition type, string fieldName) {
        foreach (var field in type.Fields) {
            if (field.Name == fieldName)
                return field;
        }
        return null;
    }

    protected CustomAttribute GetAttributeByName(string attributeName, IEnumerable<CustomAttribute> customAttributes) {
        foreach (var attribute in customAttributes) {
            if (attribute.AttributeType.FullName == attributeName)
                return attribute;
        }
        return null;
    }

    protected bool HasAttribute(string attributeName, TypeDefinition type) {
        return GetAttributeByName(attributeName, type.CustomAttributes) != null;
    }

    protected bool HasAttribute(string attributeName, MethodDefinition method) {
        return GetAttributeByName(attributeName, method.CustomAttributes) != null;
    }

    protected bool HasAttribute(string attributeName, FieldDefinition field) {
        return GetAttributeByName(attributeName, field.CustomAttributes) != null;
    }

    protected bool IsInstrutionMethodCall(Instruction instruction) {
        var checkCallOpCodes = new List<OpCode> {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        return checkCallOpCodes.Contains(instruction.OpCode);
    }

    protected bool IsDelegate(ParameterDefinition param) {
        TypeDefinition type = ResolveTypeReference(param.ParameterType);
        if (type == null || type.BaseType == null)
            return false;

        return type.BaseType.FullName == "System.MulticastDelegate";
    }

    protected bool TypeInheritsFrom(TypeDefinition type, string className) {
        if (type == null)
            return false;
        if (type.BaseType.FullName.StartsWith("System."))
            return false;

        var parentType = ResolveTypeReference(type.BaseType);

        do {
            if (parentType.FullName == className)
                return true;
            if (parentType.BaseType.FullName.StartsWith("System."))
                break;

            parentType = ResolveTypeReference(parentType.BaseType);
        } while (parentType != null);

        return false;
    }

    protected TypeDefinition ResolveTypeReference(TypeReference typeRef) {
        var typeDef = typeRef.Resolve();
        if (typeDef == null)
            throw new Exception("Cannot resolve type reference: " + typeRef);

        return typeDef;
    }

    protected MethodDefinition ResolveMethodReference(MethodReference methodRef) {
        var methodDef = methodRef.Resolve();
        if (methodDef == null)
            throw new Exception("Cannot resolve method reference: " + methodRef);

        return methodDef;
    }

    protected void CopyMethodBody(MethodDefinition from, MethodDefinition to) {
        if (to.Body.Instructions.Count > 0) {
            ClearMethodBody(to);
        }

        to.Body.InitLocals = from.Body.InitLocals;
        foreach (var variable in from.Body.Variables) {
            to.Body.Variables.Add(variable);
        }
        var il = to.Body.GetILProcessor();
        foreach (var instruction in from.Body.Instructions) {
            il.Append(instruction);
        }
    }

    protected void ClearMethodBody(MethodDefinition method) {
        var il = method.Body.GetILProcessor();
        method.Body.Instructions.Clear();
    }

    protected AssemblyDefinition ResolveAssembly(string name) {
        var assembly = MainModule.AssemblyResolver.Resolve(new AssemblyNameReference(name, new Version()));
        if (assembly == null) {
            Debug.LogError("Cannot resolve Assembly '" + name + "'");
        }
        return assembly;
    }

    protected int GetEnumValueByName(TypeDefinition type, string name) {
        if (!type.IsEnum)
            throw new InvalidOperationException();

        foreach (var field in type.Fields) {
            if (field.Name == name)
                return (int)field.Constant;
        }

        throw new Exception("Enum value not found for " + name);
    }
}
