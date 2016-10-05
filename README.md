# blqw.Convert3
全能转换器

> 对象转换，从未如此简单  
```csharp
obj.To<T>();                //转换失败,抛出异常
obj.To<T>(T defaultValue);  //转换失败,返回默认值
obj.To<T>(out succeed);     //输出转换是否成功
//下面3个是非泛型方法 
obj.To(Type outputType);
obj.To(Type outputType, object defaultValue);
obj.To(Type outputType, out succeed);
```

## 代码展示
更多示例代码: [demo](https://github.com/blqw/blqw.Convert3/blob/master/Demo/Program.cs) , [单元测试1](https://github.com/blqw/blqw.Convert3/blob/master/UnitTest/%E5%9F%BA%E6%9C%AC%E5%8A%9F%E8%83%BD%E6%B5%8B%E8%AF%95.cs)  
```csharp
//最基本
"1".To<int>();
"a".To<int>(0); //转换失败返回 0
"是".To<bool>(); //支持 "是/否" "真/假" "对/错" "t/f" "true/false" 等
byte[].To<Guid>();

//进阶
"1,2,3,4,5,6".To<int[]>();
"{\"id\":\"name\":\"blqw\"}".To<User>();
Dictionary.To<Entity>(); //键值对转实体
DataRow.To<Entity>(); //数据行转实体
DataTable.To<List<Entity>>; //数据表转实体集合

//更复杂
DataTable.To<List<NameValueCollection>>(); 
List<Dictionary<string, object>>.To<DataTable>(); 
new { ID=1, Name="blqw"}.To<User>(); //匿名类转换

//变态嵌套
Dictionary<Guid, Dictionary<int, User>>
    .To<Dictionary<string, Dictionary<DateTime, NameValueCollection>>>(); //不能理解就算了
```
## 扩展自定义转换器
> 扩展转换行为是以目标类型为参照的  
> 例如要转换为自定义类型`MyClass`就需要实现`IConvertor<MyClass>`接口  
> 或者直接继承基类 `BaseConvertor<MyClass>`,并保留默认无参构造函数  
> IOC组件会移动装载继承`IConvertor`的全部类型

### `MyClass`定义
```csharp
class MyClass
{
    public int Number { get; set; }
}
```
### 转换器代码
```csharp
class MyClassConvertor : BaseConvertor<MyClass>
{
    protected override MyClass ChangeType(ConvertContext context, object input, Type outputType, out bool success)
    {
        var i = context.Get<int>().ChangeType(context, input, typeof(int), out success);
        return success ? new MyClass() { Number = i } : null;
    }

    protected override MyClass ChangeType(ConvertContext context, string input, Type outputType, out bool success)
    {
        var i = context.Get<int>().ChangeType(context, input, typeof(int), out success);
        return success ? new MyClass() { Number = i } : null;
    }
}
```
### 测试代码
```csharp
var x = "1234".To<MyClass>(null);
Console.WriteLine(x?.Number); //1234
x = "abcd".To<MyClass>(null);
Console.WriteLine(x?.Number); //null
```



## 更新说明 
#### [3.0.1-beta]2016.10.05
* 修复bug

#### [3.0.0-beta]2016.09.30
* 重构代码,优化代码逻辑
* 完善注释
* 优化异常栈
 
#### 2016.08.04
* 修复`DataReader`转`String`错误的问题

#### 2016.08.03
* 修正空字符串转数组时会抛出异常的错误

#### 2016.07.20
* 覆盖老版IOC  

#### 2016.07.18
* 修复嵌套类型中获取类名错误的问题  

#### 2016.07.05
* 修复一个无限循环的bug  
* 修复-1转枚举失败的问题  
* 修复一个转型失败可能抛出异常的问题  
* 修复一个可空枚举转型失败的问题  
* 修复当某个类继承泛型类时,但本身不是泛型时抛出异常的问题  
* 更新IOC模块  

#### 2016.06.27
* 更新到2.1  
* 发布到nuget  

#### 2016.04.28  
* 发布2.0新版  
* 重构内部实现,代码更精简,可读性更高  
* 调整部分方法签名,更清晰  
* 优化加载模式,使用MEF注入更简单  
* 优化错误信息,更明确  

#### 2016.03.16
* 修复ioc插件bug  
  
#### 2016.02.23  
* 修复bug  

#### 2016.02.21  
* 优化获取属性值的逻辑，优先使用Literacy插件  

#### 2016.02.20  
* 重构ioc逻辑，修复部分bug  

#### 2015.11.06
* 正式发布并提供后期维护
