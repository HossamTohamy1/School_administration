using School_managment.Common.Enums;

namespace School_managment.ViewModels
{
    public class SuccessResponseViewModel<T> : ResponseViewModel<T>
    {
        public SuccessResponseViewModel(T data, string message)
        {
            Data = data;
            Message = message;
            IsSuccess = true;
            ErrorCode = ErrorCode.NoError;
        }
    }

}
