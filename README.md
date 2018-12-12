# ShaderAccessor
Define the structure, assign values to shader parameters using C# reflection,work at unity

For many variants of the shader, or will use C# to change the shader parameters at runtime. If the assignments or UI are all hand-coded, it is really inhuman. So I wrote this tool, used C# reflection for shader parameter assignment. Performance is not as good as hand-coded, but it can avoid a lot of troubles and mistakes.

The tool supports all shader parameter type assignment and lerp, supports keyword (enum or boolean) switches, and provides custom types that can be used to resolve frame animation assignment, more see "ScriptableFrameAnimation.cs".

## Examples,more see "ShaderAccessorTest.cs"

First define some types and add attributes
```C#
[Flags]
public enum Mask
{
    Group0 = 1 << 0,
    Group1 = 1 << 1,
}

[ShaderFieldGroup] //Mark this class is a collection of shader parameters
class Keywords
{
    public const string Group0Keyword = "_Group0";
    public const string Group1Keyword = "_Group1";

    [ShaderFieldKeyword(Group0Keyword, Mask.Group0)] //Mark this member as a keyword
    public bool Group0;
    
    [ShaderFieldEnumKeyword(Group0Keyword, Mask.Group0
        , Group1Keyword, Mask.Group1)] //Mark as keywords enumeration
    public Mask ModeSwitch;
}

[ShaderFieldGroup(Mask.Group0 | Mask.Group1)] //Mark this class is a collection of shader parameters
class ShaderOptions
{
    public const string FloatValueShaderName = "_FloatValue";
    public const string IntValueShaderName = "_IntValue";
    
    [ShaderField(FloatValueShaderName, Mask.Group0)] //Mark as a shader parameter
    public float floatValue;
    
    [ShaderField(IntValueShaderName, Mask.Group1)] //Mark as a shader parameter
    public int intValue;
}
```

Assignment with "ShaderAccessor"
```C#
ShaderAccessor accessor = new ShaderAccessor(typeof(ShaderOptions)); //Instantiate assignment class

Material material = GetMaterial();
ShaderOptions shaderOptions = ShaderOptions.CreateEmpty();

accessor.Copy(shaderOptions, material); //Format : void Copy(object source, Material dest)
accessor.Copy(material, shaderOptions); //Format : void Copy(Material source, object dest)

accessor.CopyWithoutKeywords(shaderOptions, material);
accessor.Copy(shaderOptions, material, member => (member.Mask & Mask.Group0) != 0); //Only copy members marked as Group0
accessor.SetGlobalValues(shaderOptions); // like Shader.SetGlobalXXXX()
```

Get the shader parameter collection, you can easily implement the automatic UI.<br>
![demo](https://github.com/JiongXiaGu/ShaderAccessor/blob/master/Assets/ShaderFieldAccessor/ui.gif "auto draw")
