// Ether.h

#ifndef _ETHER_h
#define _ETHER_h

#if defined(ARDUINO) && ARDUINO >= 100
	#include "arduino.h"
#else
	#include "WProgram.h"
#endif

#include <NativeEthernet.h>
#include <NativeEthernetUdp.h>
#include <EEPROM.h>

class Eth_UDP
{
protected:


public:
	Eth_UDP();
	~Eth_UDP();

	//intialize all the ethernet
	void Start();

	//send a byte array
	void SendUdpByte(uint8_t* _data, uint8_t _length, IPAddress _ip, uint16_t _port);

	//send a char array
	void SendUdpChar(char* _charBuf, uint8_t _length, IPAddress _ip, uint16_t _port);

	//store the ip of this module in EEPROM
	void SaveModuleIP();

	//MAC address of this module of this module
	byte mac[6] = { 0x00, 0x00, 0x56, 0x00, 0x00, 0x78 };

	// GPS listens to
	unsigned int portNMEA = 5120;

	// Autosteer listens to 
	unsigned int portModule = 8888;

	// AgIO listens to
	unsigned int portAgIO = 9999;

	char GPS_packetBuffer[256];       // buffer for receiving GGA and VTG

	//In port 5120 - only used for external gps sent via udp
	EthernetUDP NMEA;

	//In Port 8888   Out to AgIO - 9999 The main talker
	EthernetUDP AgIO;

	//Auto set on in ethernet setup
	bool isRunning = true;

	//udp sent to all on subnet set
	IPAddress moduleIP;
	//IPAddress steerModuleIP;

	//GPS/IMU/WAS module is 120
	byte moduleIdent = 120;

	//EEPROM
	int16_t EEP_NetRead;

	// if not in eeprom, overwrite
	const int EEP_Net = 2410;
};

//extern EtherClass Ether;

#endif

