namespace School_managment.Common.Enums
{
    public enum ErrorCode
    {
        NoError = 0,                
        UnknownError = 1,         
        BadRequest = 400,          
        NotFound = 404,            
        Unauthorized = 401,         
        Forbidden = 403,           
        DuplicateClass = 409,     
        DuplicateTeacher = 1002,    
        InvalidData = 1003,         
        SubjectNotFound = 1004,  
        TeacherQuotaExceeded = 1005 
    }
}
