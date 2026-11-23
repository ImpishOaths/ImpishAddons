using System;
using System.Linq;

namespace Godot;

//Copied from https://github.com/godotengine/godot-proposals/issues/2425#issuecomment-841597854

[AttributeUsage(AttributeTargets.Field)]
public class OnReady(string nodePath) : Attribute
{
    public string NodePath { get; } = nodePath;
}

public static class OnReadyGetNodeExtensions
{
    public static void GetOnReadyNodes<T>(this T node) where T: Node
    {
        var annotatedFields = typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(info => info.IsDefined(typeof(OnReady), false));
        foreach (var annotatedField in annotatedFields)
        {
            //Retrieve the NodePath
            var nodePath = annotatedField.GetCustomAttributes(typeof(OnReady), false).Cast<OnReady>().First().NodePath;
            var targetNode = node.GetNode(nodePath);
            
            //If we couldn't get anything, let the user know
            if (targetNode is null)
            {
                GD.PrintErr($"Unable to find Node at path {nodePath}");
                continue;
            }
            
            try
            {
                //Set the field
                annotatedField.SetValue(node, targetNode);
            }
            catch (ArgumentException exception)
            {
                //The node we found was of the wrong type
                GD.PrintErr($"Found Node at path {nodePath}, but was of the incorrect type. {exception.Message}");
            }
        }
    }
}