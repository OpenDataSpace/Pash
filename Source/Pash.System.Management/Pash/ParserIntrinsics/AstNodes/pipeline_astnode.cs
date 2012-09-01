﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Ast;
using Irony.Parsing;
using Pash.Implementation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Pash.ParserIntrinsics.AstNodes
{
    public class pipeline_astnode : _astnode
    {
        public readonly assignment_expression_astnode AssignmentExpression;
        public readonly expression_astnode Expression;
        public readonly command_astnode Command;
        public readonly pipeline_tail_astnode PipelineTail;

        public pipeline_astnode(AstContext astContext, ParseTreeNode parseTreeNode)
            : base(astContext, parseTreeNode)
        {
            ////        pipeline:
            ////            assignment_expression
            ////            expression   redirections_opt  pipeline_tail_opt
            ////            command   pipeline_tail_opt

            if (this.parseTreeNode.ChildNodes[0].Term == Grammar.assignment_expression)
            {
                this.AssignmentExpression = this.ChildAstNodes.Single().Cast<assignment_expression_astnode>();
            }

            else if (this.parseTreeNode.ChildNodes[0].Term == Grammar.expression)
            {
                if (this.parseTreeNode.ChildNodes.Count > 1)
                {
                    if (this.parseTreeNode.ChildNodes.Count > 2 || this.parseTreeNode.ChildNodes[1].Term != Grammar.pipeline_tail)
                    {
                        throw new NotImplementedException(this.ToString());
                    }

                    this.PipelineTail = this.ChildAstNodes[1].Cast<pipeline_tail_astnode>();
                }

                this.Expression = this.ChildAstNodes.Single().Cast<expression_astnode>();
            }

            else if (this.parseTreeNode.ChildNodes[0].Term == Grammar.command)
            {
                this.Command = this.ChildAstNodes[0].Cast<command_astnode>();

                if (this.parseTreeNode.ChildNodes.Count > 1)
                {
                    if (this.parseTreeNode.ChildNodes.Count > 2)
                    {
                        throw new NotImplementedException(this.ToString());
                    }

                    this.PipelineTail = this.ChildAstNodes[1].Cast<pipeline_tail_astnode>();
                }
            }

            else throw new InvalidOperationException(this.ToString());
        }

        internal object Execute(ExecutionContext context, ICommandRuntime commandRuntime)
        {
            object results;

            if (this.AssignmentExpression != null)
            {
                this.AssignmentExpression.Execute(context, commandRuntime);
                results = null;
            }

            else if (this.Expression != null)
            {
                results = this.Expression.Execute(context, commandRuntime);

                if (this.PipelineTail != null) throw new NotImplementedException(this.ToString());
            }

            else if (this.Command != null)
            {
                results = this.Command.Execute(context, commandRuntime);

                if (this.PipelineTail != null) throw new NotImplementedException(this.ToString());
            }

            else throw new InvalidOperationException(this.ToString());

            return results;
        }

        //        ExecutionContext subContext = context.CreateNestedContext();
        //        subContext.inputStreamReader = context.inputStreamReader;

        //        PipelineCommandRuntime subRuntime = new PipelineCommandRuntime(((PipelineCommandRuntime)commandRuntime).pipelineProcessor);

        //        var results = ChildAstNodes.First().Execute_old(subContext, subRuntime);

        //        subContext = context.CreateNestedContext();
        //        subContext.inputStreamReader = new PSObjectPipelineReader(new[] { results });

        //        subRuntime = new PipelineCommandRuntime(((PipelineCommandRuntime)commandRuntime).pipelineProcessor);
        //        return ChildAstNodes.Skip(1).First().Execute_old(subContext, subRuntime);
    }
}