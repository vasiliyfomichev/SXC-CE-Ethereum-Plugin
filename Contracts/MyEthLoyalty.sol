// solium-disable linebreak-style
pragma solidity ^0.4.24;

contract Mortal {
    address owner;
    
    constructor() public { owner = msg.sender; }
    
    function kill() public { if (msg.sender == owner) selfdestruct(owner); }
}

/// @title Online identity storage.
contract MyEthIdentity is Mortal {
    
    function addLoyaltyPoints(uint orderAmount, address recipient)  public payable {
        uint tokenAmount = orderAmount / 10;
        recipient.transfer(tokenAmount);
    }
}