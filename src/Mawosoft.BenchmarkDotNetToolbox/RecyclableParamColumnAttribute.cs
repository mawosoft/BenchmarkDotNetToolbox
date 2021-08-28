// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using BenchmarkDotNet.Configs;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class RecyclableParamsColumnAttribute : Attribute, IConfigSource
    {
        public RecyclableParamsColumnAttribute() => Config = ManualConfig.CreateEmpty().AddColumnProvider(new RecyclableParamsColumnProvider());
        public RecyclableParamsColumnAttribute(bool tryKeepParamName = true, string genericName = "Param") => Config = ManualConfig.CreateEmpty().AddColumnProvider(new RecyclableParamsColumnProvider(tryKeepParamName, genericName));
        public IConfig Config { get; }
    }
}
