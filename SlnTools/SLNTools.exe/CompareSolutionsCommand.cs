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

namespace CWDev.SLNTools
{
    using CommandLine;
    using Core;
    using Core.Merge;
    using UIKit;

    internal class CompareSolutionsCommand : Command
    {
        private class Arguments
        {
            [Argument(ArgumentType.AtMostOnce)]
            public bool IgnoreWarning = false;

            [DefaultArgument(ArgumentType.Multiple)]
            public string[] Solutions = null;
        }

        public override void Run(string[] args, MessageBoxErrorReporter reporter)
        {
            var parsedArguments = new Arguments();
            reporter.CommandUsage = Parser.ArgumentsUsage(parsedArguments.GetType());

            if (Parser.ParseArguments(args, parsedArguments, reporter.Handler))
            {
                if (parsedArguments.Solutions.Length < 2)
                {
                    reporter.Handler("Two solution files should be provided, in order:\n   Old.sln\n   New.sln");
                }

                var oldSolution = CheckForWarnings(SolutionFile.FromFile(parsedArguments.Solutions[0]), parsedArguments.IgnoreWarning);
                var newSolution = CheckForWarnings(SolutionFile.FromFile(parsedArguments.Solutions[1]), parsedArguments.IgnoreWarning);
                var difference = newSolution.CompareTo(oldSolution)
                            ?? new NodeDifference(new ElementIdentifier("SolutionFile"), OperationOnParent.Modified, null);
                using (var form = new CompareSolutionsForm(difference))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}
