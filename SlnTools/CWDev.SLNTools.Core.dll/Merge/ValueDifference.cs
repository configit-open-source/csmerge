#region License

// SLNTools
// Copyright (c) 2009
// by Christian Warren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion

using System;

namespace CWDev.SLNTools.Core.Merge
{
    public class ValueDifference : Difference
    {
        private readonly string r_oldValue;
        private readonly string r_newValue;

        public ValueDifference(
                    ElementIdentifier identifier,
                    OperationOnParent operationOnParent,
                    string oldValue,
                    string newValue)
            : base(identifier, operationOnParent)
        {
            if (oldValue == newValue)
                throw new MergeException("Cannot create a ValueDifference were 'oldValue == newValue'.");

            r_oldValue = oldValue;
            r_newValue = newValue;
        }

        public string OldValue { get { return r_oldValue; } }
        public string NewValue { get { return r_newValue; } }

        public override Conflict CompareTo(Difference destinationDifference)
        {
            if (destinationDifference == null)
                throw new ArgumentNullException("destinationDifference");
            if (!destinationDifference.Identifier.Equals(this.Identifier))
                throw new MergeException("Cannot compare differences that does not share the same identifier.");

            var source = this;
            var destination = destinationDifference as ValueDifference;
            if (destination == null)
                throw new MergeException(string.Format("Cannot compare a {0} to a {1}.", destinationDifference.GetType().Name, this.GetType().Name));
            if (source.OldValue != destination.OldValue)
                throw new MergeException("Cannot compare value differences that does not share the same 'OldValue'.");

            if (source.OperationOnParent != destination.OperationOnParent)
            {
                return new OperationTypeConflict(
                            source,
                            destination);
            }
            else if (source.NewValue != destination.NewValue)
            {
                return new ValueConflict(
                            source.Identifier,
                            source.OperationOnParent,
                            source.OldValue,
                            source.NewValue,
                            destination.NewValue);
            }
            else
            {
                return null;
            }
        }

        public override string ToString()
        {
            switch (this.OperationOnParent)
            {
                case OperationOnParent.Added:
                    return string.Format("{0} has been added with the value \"{1}\".", this.Identifier, this.NewValue);

                case OperationOnParent.Modified:
                    return string.Format("{0} value modified from \"{1}\" to \"{2}\".", this.Identifier, this.OldValue, this.NewValue);

                case OperationOnParent.Removed:
                    return string.Format("{0} has been removed. Old value = \"{1}\".", this.Identifier, this.OldValue);

                default:
                    throw new ArgumentOutOfRangeException("OperationOnParent", this.OperationOnParent.ToString(), "Invalid value");
            }
        }
    }
}
