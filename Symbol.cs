namespace MetaComputer.Runtime {
    struct Symbol {
        public string Str { get; }

        private Symbol(string s) {
            this.Str = s;
        }

        public override int GetHashCode() {
            return Str.GetHashCode();
        }

        public override bool Equals(object s) {
            switch (s) {
                case Symbol sym: return Str.Equals(sym.Str);
                default: return false;
            }
        }
    }
}
