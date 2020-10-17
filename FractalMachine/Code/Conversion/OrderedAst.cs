﻿using System;
using System.Collections.Generic;
using FractalMachine.Classes;

namespace FractalMachine.Code.Conversion
{
    public static class OrderedAstConversion
    {
        delegate void OnCallback();
        delegate void OnAddAttribute(string Attribute);

        static Linear outLin;
        static List<string> Params = new List<string>();

        public static Linear ToLinear(OrderedAst oAst)
        {
            orderedAstToLinear(oAst);
            return outLin;
        }

        static void orderedAstToLinear(OrderedAst oAst)
        {
            if (outLin == null)
                outLin = new Linear(oAst);

            Linear lin = null;
            OnCallback onEnd = null;

            bool enter = false;
            bool recordAttributes = true;

            OnAddAttribute onAddAttribute = delegate (string attr)
            {
                Params.Add(attr);
            };

            if (oAst.isDeclaration)
            {
                if (oAst.isBlockDeclaration)
                {
                    lin = new Linear(outLin, oAst);
                    lin.Op = oAst.declarationType;
                    lin.Name = Extensions.Pull(oAst.attributes);
                    lin.Attributes = oAst.attributes;

                    if (oAst.isFunction)
                    {
                        // Read parenthesis arguments
                        var parenthesis = Extensions.Pull(oAst.codes, 0);
                        injectDeclareFunctionParameters(lin, parenthesis);
                    }

                    outLin = lin;
                    enter = true;
                }
                else
                {
                    lin = new Linear(outLin, oAst);
                    lin.List();
                    lin.Op = "declare";
                    lin.Name = oAst.attributes[oAst.nAttrs - 1];
                    lin.Attributes = oAst.attributes;
                    Params.Add(lin.Name);
                }
            }
            else
            {
                if (oAst.ast.IsOperator)
                {
                    switch (oAst.Subject)
                    {
                        case "=":
                            lin = new Linear(outLin, oAst);
                            lin.Op = oAst.Subject;
                            lin.Attributes.Add(pullParams());

                            onEnd = delegate
                            {
                                lin.Attributes.Add(pullParams());
                            };

                            break;

                        case ".":
                            if (oAst.parent.Subject != ".")
                                oAst.attributes[0] = pullParams() + "." + oAst.attributes[0];
                            else
                                oAst.attributes[0] = oAst.parent.attributes[0] + "." + oAst.attributes[0];

                            recordAttributes = false;

                            break;

                        default:
                            lin = new Linear(outLin, oAst);
                            lin.Op = oAst.Subject;
                            lin.Attributes.Add(pullParams());
                            lin.Assign = "$" + oAst.getTempVar();
                            Params.Add("$" + oAst.getTempVar());

                            onEnd = delegate
                            {
                                lin.Attributes.Add(pullParams());
                            };

                            break;
                    }

                }

                if (oAst.ast.IsBlockParenthesis)
                {
                    if (oAst.IsInFunctionParenthesis)
                    {
                        if (oAst.TopFunction.isDeclaration)
                        {
                            // bisognerebbe poter accedere alla lin della funzione...
                        }
                        else
                        {
                            onEnd = delegate ()
                            {
                                lin = new Linear(outLin, oAst);
                                lin.Op = "push";
                                lin.Attributes.Add(pullParams());
                            };
                        }
                    }
                }

                if (oAst.isFunction)
                {
                    lin = new Linear(outLin, oAst);
                    lin.Op = "call";
                    lin.Name = oAst.attributes[0];
                }
            }

            if (enter)
            {
                outLin = lin;
            }

            // Prepare attributes
            if (recordAttributes)
            {
                foreach (var s in oAst.attributes)
                {
                    onAddAttribute(s);
                }
            }

            ///
            /// Child analyzing
            ///

            foreach (var code in oAst.codes)
            {
                orderedAstToLinear(code);
            }


            ///
            /// Exit
            ///

            if (onEnd != null)
                onEnd();

            if (lin != null)
                lin.List();

            if (enter)
            {
                outLin = outLin.parent;
            }
        }

        static void injectDeclareFunctionParameters(Linear lin, OrderedAst oAst)
        {
            var sett = lin.NewSetting(oAst);
            sett.Op = "parameters";

            var oa = oAst.codes[0];
            Linear l = new Linear(sett, oa);
            l.List();
            
            while (oa != null)
            {
                if (oa.Subject == ",")
                {
                    l = new Linear(sett, oa);
                    l.List();
                }

                switch (oa.Subject)
                {
                    case "=":
                        // todo: creare settings al posto che attributes (oppure creare dictionary)
                        l.Attributes = oa.attributes;
                        break;

                    default:
                        l.Name = oa.attributes[0];
                        break;
                }

                if (oa.codes.Count == 1)
                    oa = oa.codes[0];
                else
                    oa = null;
            }

        }

        static string pullParams()
        {
            var c = Params.Count - 1;

            if (c < 0)
                return "tolinParams EMPTY";

            string s = Params[c];
            Params.RemoveAt(c);
            return s;
        }
    }
}
