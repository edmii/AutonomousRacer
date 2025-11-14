"""Quick script to check CUDA availability for ML-Agents training"""
import torch

print("=" * 50)
print("CUDA Availability Check")
print("=" * 50)

cuda_available = torch.cuda.is_available()
print(f"CUDA Available: {cuda_available}")

if cuda_available:
    print(f"CUDA Device Count: {torch.cuda.device_count()}")
    print(f"Current CUDA Device: {torch.cuda.current_device()}")
    print(f"CUDA Device Name: {torch.cuda.get_device_name(0)}")
    print(f"CUDA Version: {torch.version.cuda}")
    print(f"cuDNN Version: {torch.backends.cudnn.version()}")
else:
    print("CUDA is not available. Training will use CPU.")
    print("This will be significantly slower than GPU training.")
    print("\nTo enable CUDA:")
    print("1. Install NVIDIA GPU drivers")
    print("2. Install CUDA toolkit")
    print("3. Reinstall PyTorch with CUDA support:")
    print("   pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121")

print("=" * 50)
print(f"PyTorch Version: {torch.__version__}")
print("=" * 50)

