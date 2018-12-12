# ShaderAccessor
Define the structure, assign values to shader parameters using C# reflection,work at unity

最近写了一个很多变体的着色器,而且在运行时会用到C#动态的改变着色器参数.如果全部使用手工编码,那真的是惨无人道.所以写了工具,使用C#反射进行赋值.性能肯定不如手工编码,但是能避免很多麻烦和错误.



![demo](https://github.com/JiongXiaGu/ShaderAccessor/blob/master/Assets/ShaderFieldAccessor/ui.gif)
