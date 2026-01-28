using UnityEngine;
using UnityEngine.Events;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Interface for all mini games that can be triggered in the world
    /// </summary>
    public interface IMiniGame
    {
        /// <summary>
        /// Start the mini game
        /// </summary>
        void StartGame();

        /// <summary>
        /// Stop/Exit the mini game
        /// </summary>
        void StopGame();

        /// <summary>
        /// Called when game is over
        /// </summary>
        event UnityAction OnGameExit;

        /// <summary>
        /// Is the game currently active
        /// </summary>
        bool IsActive { get; }
    }
}
