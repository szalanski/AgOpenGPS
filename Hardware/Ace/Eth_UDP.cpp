// 
// 
// 

#include "Eth_UDP.h"

//constructor
Eth_UDP::Eth_UDP(void) 
{
    moduleIP[0] = 192;
    moduleIP[1] = 168;
    moduleIP[2] = 1;
    moduleIP[3] = moduleIdent;

    EEPROM.get(60, EEP_NetRead);              // read identifier

    if (EEP_NetRead != EEP_Net)            // check on first start and write EEPROM
    {
        EEPROM.put(60, EEP_Net);
        EEPROM.put(62, moduleIP[0]);
        EEPROM.put(63, moduleIP[1]);
        EEPROM.put(64, moduleIP[2]);
    }
    else
    {
        EEPROM.get(62, moduleIP[0]);
        EEPROM.get(63, moduleIP[1]);
        EEPROM.get(64, moduleIP[2]);
    }
}

//destructor
Eth_UDP::~Eth_UDP(void) {}

//Initalize ethernet connections
void Eth_UDP::Start()
{
    // start the Ethernet connection:
    Serial.println("Initializing ethernet with static IP address");

    // Start Ethernet with IP from settings
    Ethernet.begin(mac, moduleIP); 

    Serial.print("\r\nEthernet IP of module: "); Serial.println(Ethernet.localIP());

    //set this module net IP to broadcast
    moduleIP[3] = 255;

    Serial.print("\r\nEthernet Broadcast IP: "); Serial.println(moduleIP);

    Serial.print("\r\nAgIO listening to port: "); Serial.println(portAgIO);

    // Check for Ethernet hardware present
    if (Ethernet.hardwareStatus() == EthernetNoHardware)
    {
        Serial.println("\r\nEthernet shield was not found. GPS via USB only.");
        isRunning = false;
        return;
    }

    if (Ethernet.linkStatus() == LinkOFF)
    {
        Serial.println("\r\nEthernet cable is not connected.");
    }

    Serial.println("\r\nEthernet status OK");

    // init UPD GPS Port
    if (NMEA.begin(portNMEA))
    {
        isRunning = true;
        Serial.print("Ethernet GPS UDP listening on port: ");
        Serial.println(portNMEA);
    }

    // init UPD Port getting AutoSteer data from AOG
    if (AgIO.begin(portModule)) // AOGAutoSteerPortipPort
    {
        Serial.print("Ethernet Module UDP listening to port: ");
        Serial.println(portModule);
    }
}

void Eth_UDP::SendUdpByte(uint8_t* _data, uint8_t _length, IPAddress _ip, uint16_t _port)
{
    AgIO.beginPacket(_ip, _port);
    AgIO.write(_data, _length);
    AgIO.endPacket();
}

void Eth_UDP::SendUdpChar(char* _charBuf, uint8_t _length, IPAddress _ip, uint16_t _port)
{
    AgIO.beginPacket(_ip, _port);
    AgIO.write(_charBuf, _length);
    AgIO.endPacket();
}

void Eth_UDP::SaveModuleIP(void)
{
    //ID stored in 60
    EEPROM.put(62, moduleIP[0]);
    EEPROM.put(63, moduleIP[1]);
    EEPROM.put(64, moduleIP[2]);
}



//EtherClass Ether;

