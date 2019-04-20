# ShaderAccessor

For many variants of the shader, or will use C# to change the shader parameters at runtime. If the assignments or UI are all hand-coded, it is really inhuman. So I wrote this tool, used C# reflection for shader parameter assignment. Because reflection is used, performance is not as good as hand-coded, but it can avoid a lot of troubles and mistakes.

The tool supports all shader parameter type assignment and lerp, supports keyword (enum or boolean) switches, and provides custom types that can be used to resolve frame animation assignment, more see "ScriptableFrameAnimation.cs".

## Examples,more see "ShaderAccessorTest.cs"

First define some types and add attributes
```C#
public enum Groups
{
    Group0 = 1 << 0,
    Group1 = 1 << 1,
}

[ShaderFieldGroup] //Mark this class is a collection of shader parameters
class Keywords
{
    public const string Group0Keyword = "_Group0";
    public const string Group1Keyword = "_Group1";

    [ShaderFieldKeyword(Group0Keyword, Groups.Group0)] //Mark this member as a keyword
    public bool Group0;
    
    [ShaderFieldEnumKeyword(Group0Keyword, Groups.Group0
        , Group1Keyword, Groups.Group1)] //Mark as keywords enumeration
    public Groups ModeSwitch;
}

[ShaderFieldGroup(Groups.Group0 | Groups.Group1)] //Mark this class is a collection of shader parameters
class ShaderParameters
{
    public const string FloatValueShaderName = "_FloatValue";
    public const string IntValueShaderName = "_IntValue";
    
    [ShaderField(FloatValueShaderName, Groups.Group0)] //Mark as a shader parameter
    public float floatValue;
    
    [ShaderField(IntValueShaderName, Groups.Group1)] //Mark as a shader parameter
    public int intValue {get; set;}
}

class ShaderOptions
{
    public Keywords Keywords;
    public ShaderParameters Parameters;
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
accessor.Copy(shaderOptions, material, member => (member.Mask & Groups.Group0) != 0); //Only copy members marked as Group0
accessor.SetGlobalValues(shaderOptions); // like Shader.SetGlobalXXXX()
```

Get the shader parameter collection, you can easily implement the automatic UI.<br>
![demo](https://github.com/JiongXiaGu/ShaderAccessor/blob/master/Assets/ShaderFieldAccessor/ui.gif "auto draw")
