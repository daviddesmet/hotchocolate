namespace HotChocolate.Data.Filters;

/// <summary>
/// This is an input type descriptor for lists. This is sloley used for the inline customization
/// for filtering. <see cref=""></see>
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IListOperationTypeDescriptor<T> : IFilterInputTypeDescriptor
{
}
