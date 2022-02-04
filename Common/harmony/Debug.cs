using System;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;

namespace Common.Harmony
{
	static partial class HarmonyExtensions
	{
		public static void log(this CodeInstruction ci) => $"{ci.opcode} {ci.operand}".log();

		public static void log(this IEnumerable<CodeInstruction> cins, string filename = null, bool printIndexes = true, bool printFirst = false)
		{
			StringBuilder sb = new();
			var list = cins.ToList();

			int _findLabel(object label) => // find target index for jumps
				list.FindIndex(_ci => _ci.labels?.FindIndex(l => l.Equals(label)) != -1);

			static string _label2Str(Label label) => $"Label{label.GetHashCode()}";

			static string _labelsInfo(CodeInstruction ci)
			{
				if ((ci.labels?.Count ?? 0) == 0)
					return "";

				string res = $" => labels({ci.labels.Count}): ";
				ci.labels.ForEach(l => res += _label2Str(l) + " ");

				return res;
			}

			for (int i = 0; i < list.Count; i++)
			{
				var ci = list[i];

				int labelIndex = (ci.operand is Label)? _findLabel(ci.operand): -1;
				string operandInfo = labelIndex == -1? ci.operand?.ToString(): $"jump to {(printIndexes? labelIndex.ToString(): "")}";
				string isFirstOp = (printFirst && list.FindIndex(_ci => _ci.opcode == ci.opcode) == i)? " 1ST":""; // is such an opcode is first encountered in this instruction
				string prefix = printIndexes? $"{i:D3}{isFirstOp}: ": "";

				sb.AppendLine($"{prefix}{ci.opcode} {operandInfo}{_labelsInfo(ci)}");
			}

			if (filename == null)
			{
				sb.Insert(0, Environment.NewLine);
				sb.ToString().log();
			}
			else
			{
				sb.ToString().saveToFile(filename);
			}
		}
	}
}