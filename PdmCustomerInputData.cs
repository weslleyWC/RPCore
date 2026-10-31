namespace RPCore
{
    public sealed class PdmCustomerPulse
    {
        int frame;bool seen,held,available;
        public void Sample(int currentFrame,bool keyboardDown,bool controllerPressed)
        {
            if(seen && frame==currentFrame)return;
            available=keyboardDown && !held || controllerPressed;held=keyboardDown;frame=currentFrame;seen=true;
        }
        public void Block()=>available=false;
        public bool Take(bool eligible){if(!eligible || !available)return false;available=false;return true;}
    }
}
