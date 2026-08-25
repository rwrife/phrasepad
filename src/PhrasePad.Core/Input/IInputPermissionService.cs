namespace PhrasePad.Core.Input;

public interface IInputPermissionService
{
    InputPermissionStatus GetStatus();

    void OpenSettings();
}
