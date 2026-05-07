using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Runtime.InteropServices;

namespace TriadBuddyPlugin;

public class UnsafeReaderTriadCards
{
    public bool HasErrors => !CanReadOwnedCards && !CanReadNpcProgress;
    public bool CanReadNpcProgress => IsNpcBeatenFunc != null;
    public unsafe bool CanReadOwnedCards => UIState.Instance() != null;

    private delegate byte IsNpcBeatenDelegate(IntPtr uiState, int triadNpcId);

    private readonly IsNpcBeatenDelegate? IsNpcBeatenFunc;

    public UnsafeReaderTriadCards()
    {
        var isNpcBeatenPtr = IntPtr.Zero;
        var sigScanner = Service.sigScanner;

        if (sigScanner != null)
        {
            try
            {
                // IsTriadNpcCompleted(void* uiState, int triadNpcId)
                //   identified by pretty unique rowId from TripleTriad sheet: 0x230002
                //   looking for negative of that number (0xFFDCFFFE) gives pretty much only npc access functions (set + get)

                isNpcBeatenPtr = sigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 0F 94 C0 88 43 58 45 33 FF");
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "oh noes!");
            }
        }

        if (isNpcBeatenPtr != IntPtr.Zero)
        {
            IsNpcBeatenFunc = Marshal.GetDelegateForFunctionPointer<IsNpcBeatenDelegate>(isNpcBeatenPtr);
        }
        else
        {
            Svc.Log.Warning("Failed to find triad NPC progress function, NPC completion tracking disabled");
        }
    }

    public unsafe bool IsCardOwned(int cardId)
    {
        var uiState = UIState.Instance();
        if (uiState == null || cardId <= 0 || cardId > 65535)
        {
            return false;
        }

        return uiState->IsTripleTriadCardUnlocked((ushort)cardId);
    }

    public unsafe bool IsNpcBeaten(int npcId)
    {
        var uiState = UIState.Instance();
        if (IsNpcBeatenFunc == null || uiState == null || npcId < 0x230002)
        {
            return false;
        }

        return IsNpcBeatenFunc((IntPtr)uiState, npcId) != 0;
    }

    /*public void TestBeatenNpcs()
    {
        // fixed addr from 5.58
        IntPtr memAddr = (IntPtr)UIState.Instance() + 0x15d18;

        byte[] flags = Dalamud.Memory.MemoryHelper.ReadRaw(memAddr, 0x70 / 8);
        flags[10 / 8] |= 1 << (10 % 8);
        flags[11 / 8] |= 1 << (11 % 8);
        flags[12 / 8] |= 1 << (12 % 8);

        Dalamud.Memory.MemoryHelper.WriteRaw(memAddr, flags);
    }*/

    /*public void TestOwnedCardBits()
    {
        // fixed addr from 5.58
        IntPtr memAddr = (IntPtr)UIState.Instance() + 0x15ce5;

        byte[] flags = Dalamud.Memory.MemoryHelper.ReadRaw(memAddr, 0x29);
        flags[70 / 8] |= 1 << (70 % 8);
        flags[71 / 8] |= 1 << (71 % 8);
        flags[72 / 8] |= 1 << (72 % 8);

        Dalamud.Memory.MemoryHelper.WriteRaw(memAddr, flags);
    }*/
}
