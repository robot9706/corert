﻿
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class LocalMethodImport : DelayLoadHelperImport, IMethodNode
    {
        private readonly SignatureContext _signatureContext;

        private readonly MethodWithGCInfo _localMethod;

        public LocalMethodImport(
            ReadyToRunCodegenNodeFactory factory,
            ReadyToRunFixupKind fixupKind,
            MethodWithGCInfo localMethod,
            bool isUnboxingStub,
            SignatureContext signatureContext)
            : base(
                  factory,
                  factory.MethodImports,
                  ReadyToRunHelper.READYTORUN_HELPER_DelayLoad_MethodCall,
                  factory.MethodSignature(
                      fixupKind,
                      localMethod.Method,
                      constrainedType: null,
                      methodToken: default(ModuleToken),
                      signatureContext,
                      isUnboxingStub,
                      isInstantiatingStub: false))
        {
            _signatureContext = signatureContext;
            _localMethod = localMethod;
        }

        public MethodDesc Method => _localMethod.Method;
        public MethodWithGCInfo MethodCodeNode => _localMethod;

        public override int ClassCode => 459923351;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            foreach (DependencyListEntry entry in base.GetStaticDependencies(factory))
            {
                yield return entry;
            }
            yield return new DependencyListEntry(_localMethod, "Local method import");
        }
    }
}
