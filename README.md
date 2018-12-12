# ShaderAccessor
Define the structure, assign values to shader parameters using C# reflection,work at unity

最近写了一个很多变体的着色器,而且在运行时会用到C#动态的改变着色器参数.如果全部使用手工编码,那真的是惨无人道.所以写了工具,使用C#反射进行赋值.性能肯定不如手工编码,但是能避免很多麻烦和错误.

#部分演示,更多你可以看这儿

First define some types and add attributes
```C#
[Flags]
public enum Mask
{
    Group0 = 1 << 0,
    Group1 = 1 << 1,
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

accessor.Copy(shaderOptions, material, member => (member.Mask & Mask.Group0) != 0); //Only copy members marked as Group0
```



![demo](https://github.com/JiongXiaGu/ShaderAccessor/blob/master/Assets/ShaderFieldAccessor/ui.gif)
