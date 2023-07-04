using Discord.Interactions;

namespace Bastian.Modules.Polls.Enums;
public enum VoteType
{
    Buttons,
    [ChoiceDisplay("Select Menu")]
    SelectMenu
}