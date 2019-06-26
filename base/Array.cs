namespace Roboot.Base {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Roboot.Util;
    using Roboot.Runtime;

    public class FlatImmutableList<T> {
        private readonly Lazy<IImmutableList<T>> asTree;

        private readonly T[] data;
    }

    public class FlatImmutableSlice<T> {
        private readonly FlatImmutableList<T> list;

        private readonly int start;
        private readonly int end;
    }
}
