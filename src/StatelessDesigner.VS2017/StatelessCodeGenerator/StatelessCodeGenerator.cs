﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using StatelessXml;
using VSLangProj80;

namespace StatelessCodeGenerator
{
  [ComVisible(true)]
  [ClassInterface(ClassInterfaceType.None)]
  [Guid("A4008ECD-02C6-418F-A0BD-083461479064")]
  [CodeGeneratorRegistration(typeof(StatelessCodeGenerator),
      "Stateless Designer",
      vsContextGuids.vsContextGuidVCSProject,
      GeneratesDesignTimeSource = true)]
  [ProvideObject(typeof(StatelessCodeGenerator))]
  public class StatelessCodeGenerator : BaseCodeGeneratorWithSite
  {
    public override string GetDefaultExtension()
    {
      return ".cs";
    }

    protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
    {
      try
      {
        var xmlParser = new XmlParser(inputFileContent);
        var itemname = xmlParser.ItemName;
        var ns = xmlParser.NameSpace;
        var triggers = xmlParser.Triggers;
        var states = xmlParser.States;
        var startstate = xmlParser.StartState;
        var transitions = xmlParser.Transitions;

        var sb = new StringBuilder();
        sb.Append(
          "// IMPORTANT: THIS IS MACHINE-GENERATED CODE" + Environment.NewLine +
          "// PLEASE DO NOT EDIT" + Environment.NewLine +
          "// Generated by Stateless Designer" + Environment.NewLine +
          "// http://statelessdesigner.codeplex.com/" + Environment.NewLine +
          Environment.NewLine +
          "using Stateless;" + Environment.NewLine +
          Environment.NewLine
          );
        sb.Append("namespace ");
        sb.Append(ns);
        sb.Append(Environment.NewLine);
        sb.Append("{" + Environment.NewLine);
        sb.Append("  ");
        sb.Append(xmlParser.ClassType);
        sb.Append(" class ");
        sb.Append(itemname);
        sb.Append(Environment.NewLine);
        sb.Append(
          "  {" + Environment.NewLine +
          "    public delegate void UnhandledTriggerDelegate(State state, Trigger trigger);" + Environment.NewLine +
          "    public delegate void EntryExitDelegate();" + Environment.NewLine +
          "    public delegate bool GuardClauseDelegate();" + Environment.NewLine +
          Environment.NewLine +
          "    public enum Trigger" + Environment.NewLine +
          "    {" + Environment.NewLine
          );
        sb.Append(triggers.Aggregate("",
                                     (current, trigger) =>
                                     current + ("      " + trigger + "," + Environment.NewLine)));
        sb.Append("    }" + Environment.NewLine + Environment.NewLine);

        sb.Append(
          "    public enum State" + Environment.NewLine +
          "    {" + Environment.NewLine
          );
        sb.Append(states.Aggregate("",
                                     (current, state) =>
                                     current + ("      " + state + "," + Environment.NewLine)));
        sb.Append("    }" + Environment.NewLine + Environment.NewLine);

        sb.Append(
          "    private readonly StateMachine<State, Trigger> stateMachine = null;" + Environment.NewLine +
          Environment.NewLine);

        foreach (var state in states)
        {
          sb.Append("    public EntryExitDelegate On");
          sb.Append(state);
          sb.Append("Entry = null;" + Environment.NewLine);
          sb.Append("    public EntryExitDelegate On");
          sb.Append(state);
          sb.Append("Exit = null;" + Environment.NewLine);
        }
        foreach (var state in states)
        {
          var tr = from t in transitions
                   where t.From == state
                   select t;
          foreach (var t in tr)
          {
            sb.Append("    public GuardClauseDelegate GuardClauseFrom");
            sb.Append(t.From);
            sb.Append("To");
            sb.Append(t.To);
            sb.Append("UsingTrigger");
            sb.Append(t.Trigger);
            sb.Append(" = null;" + Environment.NewLine);
          }
        }
        sb.Append("    public UnhandledTriggerDelegate OnUnhandledTrigger = null;" + Environment.NewLine);

        sb.Append(Environment.NewLine);
        sb.Append("    public ");
        sb.Append(itemname);
        sb.Append(
          "()" + Environment.NewLine +
          "    {" + Environment.NewLine +
          "      stateMachine = new StateMachine<State, Trigger>(State.");
        sb.Append(startstate);
        sb.Append(");" + Environment.NewLine);

        foreach (var state in states)
        {
          sb.Append("      stateMachine.Configure(State.");
          sb.Append(state);
          sb.Append(")" + Environment.NewLine);

          sb.Append("        .OnEntry(() => { if (On");
          sb.Append(state);
          sb.Append("Entry != null) On");
          sb.Append(state);
          sb.Append("Entry(); })");
          sb.Append(Environment.NewLine);

          sb.Append("        .OnExit(() => { if (On");
          sb.Append(state);
          sb.Append("Exit != null) On");
          sb.Append(state);
          sb.Append("Exit(); })");
          sb.Append(Environment.NewLine);

          var tr = from t in transitions
                   where t.From == state
                   select t;
          foreach (var t in tr)
          {
            var gaudClauseDelegate = string.Format("GuardClauseFrom{0}To{1}UsingTrigger{2}", t.From, t.To, t.Trigger);
            if (state == t.To)
            {
              sb.Append("        .PermitReentryIf(Trigger.");
              sb.Append(t.Trigger);
              sb.Append(" , () => { if (");
              sb.Append(gaudClauseDelegate);
              sb.Append(" != null) return ");
              sb.Append(gaudClauseDelegate);
              sb.Append("(); return true; } )");
              sb.Append(Environment.NewLine);
            }
            else
            {
              sb.Append("        .PermitIf(Trigger.");
              sb.Append(t.Trigger);
              sb.Append(", State.");
              sb.Append(t.To); 
              sb.Append(" , () => { if (");
              sb.Append(gaudClauseDelegate);
              sb.Append(" != null) return ");
              sb.Append(gaudClauseDelegate);
              sb.Append("(); return true; } )");
              sb.Append(Environment.NewLine);
            }
          }
          sb.Append("      ;" + Environment.NewLine);

        }
        sb.Append("      stateMachine.OnUnhandledTrigger((state, trigger) => { if (OnUnhandledTrigger != null) OnUnhandledTrigger(state, trigger); });" + Environment.NewLine);

        sb.Append("    }" + Environment.NewLine);
        sb.Append(Environment.NewLine);

        sb.Append(
          "    public bool TryFireTrigger(Trigger trigger)" + Environment.NewLine +
          "    {" + Environment.NewLine +
          "      if (!stateMachine.CanFire(trigger))" + Environment.NewLine +
          "      {" + Environment.NewLine +
          "        return false;" + Environment.NewLine +
          "      }" + Environment.NewLine +
          "      stateMachine.Fire(trigger);" + Environment.NewLine +
          "      return true;" + Environment.NewLine +
          "    }" + Environment.NewLine);

        sb.Append(
          Environment.NewLine +

          "    public State GetState" + Environment.NewLine +
          "    {" + Environment.NewLine +
          "      get" + Environment.NewLine +
          "      {" + Environment.NewLine +
          "        return stateMachine.State;" + Environment.NewLine +
          "      }" + Environment.NewLine +
          "    }" + Environment.NewLine +

          "  }" + Environment.NewLine +
          "}");

        return Encoding.ASCII.GetBytes(sb.ToString());
      }
      catch (Exception ex)
      {
        return Encoding.ASCII.GetBytes(ex.Message);
      }
    }
  }
}