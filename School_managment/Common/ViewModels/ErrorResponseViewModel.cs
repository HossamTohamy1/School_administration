using School_managment.Common.Enums;
using School_managment.Common.Enums; // لو عندك enum للأخطاء، أو ممكن تعمل واحدة

namespace School_managment.ViewModels
{
  

    public class ErrorResponseViewModel<T> : ResponseViewModel<T>
    {
        public ErrorResponseViewModel(string message, ErrorCode errorCode = ErrorCode.UnknownError)
        {
            Data = default;
            IsSuccess = false;
            Message = message;
            ErrorCode = errorCode;
        }
    }
}
