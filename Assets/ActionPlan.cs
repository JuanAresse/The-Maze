using System;
using System.Collections.Generic;

public enum ActionKind { Up, Down, Left, Right, Wait, Repeat, Shoot }
public enum RadarAdjustment { TurnLeft, TurnRight, Forward, Backward, Left, Right }

[Serializable]
public class ActionBlock
{
    public ActionKind Kind;
    public int Repetitions = 2;
    public bool UsePhase;
    public bool UseTrueRadar;
    public List<ActionBlock> Children = new List<ActionBlock>();
    public List<RadarAdjustment> Adjustments = new List<RadarAdjustment>();

    public ActionBlock(ActionKind kind) { Kind = kind; }
    public bool IsMovement => Kind >= ActionKind.Up && Kind <= ActionKind.Right;

}

public sealed class PlanStep
{
    public ActionBlock Block;
    public readonly List<ActionBlock> Parents = new List<ActionBlock>();
}

public sealed class PlanValidation
{
    public int Actions;
    public int Shots;
    public string Error = "";
    public bool IsValid => Error.Length == 0;
    public readonly List<PlanStep> Steps = new List<PlanStep>();
}

public static class ActionPlan
{
    public const int MaxActions = 8;
    public const int MaxShots = 1;
    public const int MaxDepth = 6;
    public const int MaxBlocks = 64;
    public const string FirstTurnShotError = "Cada jugador puede disparar a partir de su segundo turno.";

    public static PlanValidation Validate(List<ActionBlock> blocks, bool allowShots = true)
    {
        var result = new PlanValidation();
        int stored = 0;
        Count(blocks, result, 0, new HashSet<ActionBlock>(), ref stored);
        if (result.Error.Length == 0 && result.Actions == 0) result.Error = "Añadí una acción o elegí Pasar turno.";
        if (result.Error.Length == 0 && !allowShots && result.Shots > 0) result.Error = FirstTurnShotError;
        if (result.Error.Length == 0 && result.Actions > MaxActions) result.Error = "La ruta supera las 8 acciones por turno. Las repeticiones también cuentan.";
        if (result.Error.Length == 0 && result.Shots > MaxShots) result.Error = "Solo se permite un disparo por turno, también dentro de Repetir.";
        if (result.IsValid) Expand(blocks, new List<ActionBlock>(), result.Steps);
        return result;
    }

    private static void Count(List<ActionBlock> blocks, PlanValidation result, int depth, HashSet<ActionBlock> path, ref int stored)
    {
        if (blocks == null || depth > MaxDepth) { result.Error = "La repetición tiene demasiados niveles."; return; }
        foreach (var block in blocks)
        {
            if (block == null || !Enum.IsDefined(typeof(ActionKind), block.Kind) || !path.Add(block))
            { result.Error = "La ruta contiene un bloque inválido."; return; }
            if (++stored > MaxBlocks) { result.Error = "La ruta contiene demasiados bloques."; return; }
            if (block.Kind == ActionKind.Repeat)
            {
                if (block.Repetitions < 1 || block.Repetitions > MaxActions || block.Children == null || block.Children.Count == 0)
                { result.Error = "Cada Repetir necesita acciones y una cantidad entre 1 y 8."; return; }
                var nested = new PlanValidation();
                Count(block.Children, nested, depth + 1, path, ref stored);
                if (!nested.IsValid) { result.Error = nested.Error; return; }
                // Saturation avoids integer overflow and prevents expanding oversized plans.
                result.Actions = Math.Min(1000, result.Actions + nested.Actions * block.Repetitions);
                result.Shots = Math.Min(1000, result.Shots + nested.Shots * block.Repetitions);
            }
            else
            {
                result.Actions = Math.Min(1000, result.Actions + 1);
                if (block.Kind == ActionKind.Shoot) result.Shots = Math.Min(1000, result.Shots + 1);
            }
            path.Remove(block);
        }
    }

    private static void Expand(List<ActionBlock> blocks, List<ActionBlock> parents, List<PlanStep> steps)
    {
        foreach (var block in blocks)
        {
            if (block.Kind == ActionKind.Repeat)
            {
                parents.Add(block);
                for (int i = 0; i < block.Repetitions; i++) Expand(block.Children, parents, steps);
                parents.RemoveAt(parents.Count - 1);
            }
            else
            {
                var step = new PlanStep { Block = block };
                step.Parents.AddRange(parents);
                steps.Add(step);
            }
        }
    }

    public static bool Contains(ActionBlock parent, ActionBlock candidate)
    {
        if (parent == candidate) return true;
        foreach (var child in parent.Children) if (Contains(child, candidate)) return true;
        return false;
    }

    public static string Label(ActionKind kind)
    {
        switch (kind)
        {
            case ActionKind.Up: return "Arriba";
            case ActionKind.Down: return "Abajo";
            case ActionKind.Left: return "Izquierda";
            case ActionKind.Right: return "Derecha";
            case ActionKind.Wait: return "Esperar";
            case ActionKind.Repeat: return "Repetir";
            default: return "Disparar";
        }
    }

    public static string AdjustmentLabel(RadarAdjustment adjustment)
    {
        switch (adjustment)
        {
            case RadarAdjustment.TurnLeft: return "Girar radar a izquierda";
            case RadarAdjustment.TurnRight: return "Girar radar a derecha";
            case RadarAdjustment.Forward: return "Radar adelante";
            case RadarAdjustment.Backward: return "Radar atrás";
            case RadarAdjustment.Left: return "Radar a izquierda";
            default: return "Radar a derecha";
        }
    }
}
