extern "C" {
    __declspec(dllexport) int __stdcall asiomain(void(__stdcall *asio_callback)(void*, int, int, int));
}