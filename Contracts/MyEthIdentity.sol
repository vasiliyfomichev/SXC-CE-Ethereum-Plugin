// solium-disable linebreak-style
pragma solidity ^0.4.24;

contract Mortal {
    address owner;
    
    constructor() public { owner = msg.sender; }
    
    function kill() public { if (msg.sender == owner) selfdestruct(owner); }
}

/// @title Online identity storage.
contract MyEthIdentity is Mortal {
    
    Identity public myIdentity;

    function createIdentity(string firstName, string lastName, string contactId) public {
        Identity memory identity = Identity({
            firstName: firstName,
            lastName: lastName,
            registree: msg.sender,
            contactId: contactId,
            purchasedProductIds: new string[](0),
            ownedProductIds: new string[](0)
        });
        myIdentity = identity;
    }
    
    function identityExists() public view returns (bool){
        if(keccak256(abi.encodePacked(myIdentity.contactId)) == keccak256(abi.encodePacked("")))
        {
            return false;
        }
        return true;
    }

    function addPurchasedProduct(string productId) public  {
        myIdentity.purchasedProductIds.push(productId);
    }
    
    function addOwnedProduct(string productId) public  {
        myIdentity.ownedProductIds.push(productId);
    }

    function contactHasPurchasedProduct(string productId) public view returns (bool){
        bool hasProduct = false;
        for (uint p = 0; p < myIdentity.purchasedProductIds.length; p++) {
            if(keccak256(abi.encodePacked(myIdentity.purchasedProductIds[p])) == keccak256(abi.encodePacked(productId)))
            {
                hasProduct = true;
                break;
            }
        }
        return hasProduct;
    }
    
    function contactOwnsProduct(string productId) public view returns (bool){
        bool hasProduct = false;
        for (uint p = 0; p < myIdentity.ownedProductIds.length; p++) {
            if(keccak256(abi.encodePacked(myIdentity.ownedProductIds[p])) == keccak256(abi.encodePacked(productId)))
            {
                hasProduct = true;
                break;
            }
        }
        return hasProduct;
    }
    
    

    
    struct Identity {
        address registree;
        string contactId;
        string firstName;
        string lastName;
        string[] purchasedProductIds;
        string[] ownedProductIds;
    }
}

