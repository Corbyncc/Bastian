using Discord.Interactions;

#nullable disable

namespace Bastian.Modules.Polls.Modals;
public class PollAddOptionModal : IModal
{
    public string Title => "Add Poll Option";

    [InputLabel("Option")]
    [ModalTextInput("pollOption", placeholder: "Add Option", maxLength: 80)]
    public string Option { get; set; }
}