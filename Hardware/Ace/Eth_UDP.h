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

	//udp sent to all on subnet set
	IPAddress ipAddress;
	IPAddress steerAddress;

	//GPS/IMU/WAS module is x.x.x.121
	byte thisIP = 121;

	//MAC address of this module of this module
	byte mac[6] = { 0x00, 0x00, 0x56, 0x00, 0x00, 0x78 };


	// GPS listens to
	unsigned int portNMEA_5120 = 5120;

	// Autosteer listens to 
	unsigned int portModule_8888 = 8888;

	//This module listens to AgIO hello
	unsigned int portHello_7777 = 7777;

	// AgIO listens to
	unsigned int portAgIO_9999 = 9999;



	//In port 5120 - only used for external gps sent via udp
	EthernetUDP NMEA;

	//Port 8888   Out to Modules
	EthernetUDP PGN;

	//AgIO sends hello and scan on 7777
	EthernetUDP Hello;

	//Auto set on in ethernet setup
	bool isRunning = true;

	//EEPROM
	int16_t EEP_NetRead;

	// if not in eeprom, overwrite
	const int EEP_Net = 2410;


	//intialize all the ethernet
	void Start();

	//send a byte array
	void SendUdpByte(uint8_t* _data, uint8_t _length, IPAddress _ip, uint16_t _port);

	//send a char array
	void SendUdpChar(char* _charBuf, uint8_t _length, IPAddress _ip, uint16_t _port);

	//store the ip of this module in EEPROM
	void SaveModuleIP();

};

//extern EtherClass Ether;

#endif

