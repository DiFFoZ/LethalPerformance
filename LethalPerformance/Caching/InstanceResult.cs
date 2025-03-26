using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalPerformance.Caching;
internal struct InstanceResult
{
    public bool isFound;
    public Behaviour? instance;

    public InstanceResult(bool isFound, Behaviour? instance)
    {
        this.isFound = isFound;
        this.instance = instance;
    }

    public static InstanceResult Found(Behaviour? instance)
    {
        return new()
        {
            instance = instance,
            isFound = true
        };
    }

    public static InstanceResult NotFound(Behaviour? instance)
    {
        return new()
        {
            instance = instance,
            isFound = false
        };
    }

    public readonly void Deconstruct(out bool isFound, out Behaviour? instance)
    {
        isFound = this.isFound;
        instance = this.instance;
    }
}

internal struct InstancesResult
{
    public bool isFound;
    public Behaviour[]? instances;

    public InstancesResult(bool isFound, Behaviour[]? instances)
    {
        this.isFound = isFound;
        this.instances = instances;
    }

    public static InstancesResult Found(Behaviour[]? instances)
    {
        return new()
        {
            instances = instances,
            isFound = true
        };
    }

    public static InstancesResult NotFound(Behaviour[]? instances)
    {
        return new()
        {
            instances = instances,
            isFound = false
        };
    }

    public readonly void Deconstruct(out bool isFound, out Behaviour[]? instances)
    {
        isFound = this.isFound;
        instances = this.instances;
    }
}
